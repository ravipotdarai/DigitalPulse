import { useEffect, useRef, useState } from "react";
import { Button } from "../design/Button";
import { Field, SelectField } from "../design/Field";
import {
  ensureHubBlocks,
  hubBlockId,
  isSafeHref,
  parseHubMarkup,
  serializeHubMarkup,
  wrapSelection,
  type HubBlock
} from "../lib/hubMarkup";
import type { HubContentSummary, HubMediaAsset } from "../lib/api";

const KINDS = [
  ["heading", "Heading"],
  ["paragraph", "Text"],
  ["list", "List"],
  ["quote", "Quote"],
  ["image", "Image"],
  ["video", "Video"]
] as const;

export function HubRichEditor({
  value,
  onChange,
  media,
  articles,
  businessId,
  onUpload
}: {
  value: string;
  onChange: (body: string) => void;
  media: HubMediaAsset[];
  articles: HubContentSummary[];
  businessId: string;
  onUpload?: (file: File) => Promise<HubMediaAsset>;
}) {
  const sent = useRef(value);
  const focus = useRef<{ id: string; start: number; end: number } | null>(null);
  const [blocks, setBlocks] = useState(() => ensureHubBlocks(parseHubMarkup(value)));
  const [linkId, setLinkId] = useState(articles[0]?.id ?? "");
  const [note, setNote] = useState<string | null>(null);

  useEffect(() => {
    if (value === sent.current) return;
    setBlocks(ensureHubBlocks(parseHubMarkup(value)));
    sent.current = value;
  }, [value]);

  function emit(next: HubBlock[]) {
    const ready = ensureHubBlocks(next);
    setBlocks(ready);
    const text = serializeHubMarkup(ready);
    sent.current = text;
    onChange(text);
  }

  function add(type: HubBlock["type"]) {
    setNote(null);
    if (type === "list") emit([...blocks, { id: hubBlockId(), type: "list", items: [""] }]);
    else if (type === "image" || type === "video") emit([...blocks, { id: hubBlockId(), type, text: "", url: "" }]);
    else emit([...blocks, { id: hubBlockId(), type, text: "" }]);
  }

  function patch(id: string, next: HubBlock) {
    emit(blocks.map((block) => (block.id === id ? next : block)));
  }

  function move(id: string, delta: number) {
    const index = blocks.findIndex((block) => block.id === id);
    const target = index + delta;
    if (index < 0 || target < 0 || target >= blocks.length) return;
    const copy = [...blocks];
    const [item] = copy.splice(index, 1);
    copy.splice(target, 0, item);
    emit(copy);
  }

  function format(before: string, after: string) {
    const current = focus.current;
    const block = blocks.find((item) => item.id === current?.id);
    if (!block || block.type === "image" || block.type === "video") {
      setNote("Select text in a heading, paragraph, list, or quote first.");
      return;
    }
    const source = block.type === "list" ? block.items.join("\n") : block.text;
    const wrapped = wrapSelection(source, current?.start ?? 0, current?.end ?? source.length, before, after);
    patch(block.id, block.type === "list" ? { ...block, items: wrapped.text.split("\n") } : { ...block, text: wrapped.text });
  }

  function insertLink() {
    const article = articles.find((item) => item.id === linkId);
    if (!article) {
      setNote("Save another article on this business before linking internally.");
      return;
    }
    const href = `/hub/${businessId}/${article.slug}`;
    if (!isSafeHref(href)) {
      setNote("That public path is not a hub link.");
      return;
    }
    format("[", `](${href})`);
    setNote("Internal link wrapped around the selection.");
  }

  return (
    <div className="hub-editor">
      <div className="hub-editor-bar" role="toolbar" aria-label="Article blocks">
        {KINDS.map(([type, label]) => (
          <button key={type} type="button" className="studio-tab" onClick={() => add(type)}>{label}</button>
        ))}
      </div>
      <div className="hub-editor-bar" role="toolbar" aria-label="Inline format">
        <button type="button" className="studio-tab" onClick={() => format("**", "**")}>Bold</button>
        <button type="button" className="studio-tab" onClick={() => format("*", "*")}>Italic</button>
        <button type="button" className="studio-tab" onClick={insertLink}>Link selection</button>
      </div>
      <p className="ink-muted">Inline marks are stored as **bold**, *italic*, and [links](/hub/…). Preview never uses HTML from the article.</p>
      {blocks.map((block, index) => (
        <article key={block.id} className={`hub-block is-${block.type}`}>
          <header className="hub-block-meta">
            <SelectField
              label="Block"
              value={block.type}
              onChange={(type) => patch(block.id, convert(block, type as HubBlock["type"]))}
              options={KINDS.map(([value, label]) => ({ value, label }))}
            />
            <div className="hub-block-tools">
              <Button appearance="subtle" type="button" disabled={index === 0} onClick={() => move(block.id, -1)}>Up</Button>
              <Button appearance="subtle" type="button" disabled={index === blocks.length - 1} onClick={() => move(block.id, 1)}>Down</Button>
              <Button appearance="subtle" type="button" disabled={blocks.length === 1} onClick={() => emit(blocks.filter((item) => item.id !== block.id))}>Remove</Button>
            </div>
          </header>
          <BlockFields
            block={block}
            media={media}
            onUpload={onUpload}
            onFocus={(start, end) => { focus.current = { id: block.id, start, end }; }}
            onChange={(next) => patch(block.id, next)}
          />
        </article>
      ))}
      <div className="hub-editor-link">
        <SelectField
          label="Internal link"
          value={linkId}
          onChange={setLinkId}
          options={articles.filter((item) => item.slug).map((item) => ({ value: item.id, label: item.title }))}
        />
        <Button appearance="subtle" type="button" disabled={!linkId} onClick={insertLink}>Wrap selection as hub link</Button>
      </div>
      {note ? <p className="ink-muted">{note}</p> : null}
    </div>
  );
}

function BlockFields({
  block,
  media,
  onUpload,
  onFocus,
  onChange
}: {
  block: HubBlock;
  media: HubMediaAsset[];
  onUpload?: (file: File) => Promise<HubMediaAsset>;
  onFocus: (start: number, end: number) => void;
  onChange: (block: HubBlock) => void;
}) {
  const [busy, setBusy] = useState(false);

  if (block.type === "image" || block.type === "video") {
    const images = media.filter((item) => item.sourceUrl);
    return (
      <div className="hub-block-media">
        {block.type === "image" && images.length > 0 ? (
          <SelectField
            label="Registered image"
            value={images.some((item) => item.sourceUrl === block.url) ? block.url : "none"}
            onChange={(url) => onChange({ ...block, url: url === "none" ? "" : url, text: block.text || images.find((item) => item.sourceUrl === url)?.label || "" })}
            options={[{ value: "none", label: "Paste https URL or upload" }, ...images.map((item) => ({ value: item.sourceUrl!, label: item.label }))]}
          />
        ) : null}
        {block.type === "image" && onUpload ? (
          <label className="dp-field">
            <span>Upload image</span>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,image/gif"
              disabled={busy}
              onChange={(event) => {
                const file = event.target.files?.[0];
                if (!file) return;
                setBusy(true);
                void onUpload(file)
                  .then((asset) => onChange({ ...block, url: asset.sourceUrl ?? "", text: block.text || asset.label }))
                  .finally(() => setBusy(false));
              }}
            />
          </label>
        ) : null}
        <Field
          label={block.type === "image" ? "Image URL" : "YouTube or Vimeo URL"}
          type="url"
          value={block.url}
          onChange={(url) => onChange({ ...block, url })}
          hint={block.type === "image" ? "Uploaded files are served from this host. Remote URLs must be public https." : "YouTube and Vimeo become an allowlisted embed. Other hosts stay a link."}
        />
        <Field
          label={block.type === "image" ? "Alt text" : "Video title"}
          value={block.text}
          onChange={(text) => onChange({ ...block, text })}
        />
        {block.type === "image" && isSafeHref(block.url) ? <img className="hub-featured is-thumb" src={block.url} alt="" /> : null}
      </div>
    );
  }

  const text = block.type === "list" ? block.items.join("\n") : block.text;
  return (
    <label className="dp-field">
      <span className={block.type === "list" ? undefined : "visually-hidden"}>{block.type === "list" ? "Items" : block.type}</span>
      <textarea
        className={block.type === "heading" ? "hub-block-write is-heading" : "hub-block-write"}
        rows={block.type === "heading" ? 2 : Math.max(4, text.split("\n").length + 1)}
        value={text}
        onSelect={(event) => onFocus(event.currentTarget.selectionStart, event.currentTarget.selectionEnd)}
        onKeyUp={(event) => onFocus(event.currentTarget.selectionStart, event.currentTarget.selectionEnd)}
        onClick={(event) => onFocus(event.currentTarget.selectionStart, event.currentTarget.selectionEnd)}
        onChange={(event) => {
          onFocus(event.currentTarget.selectionStart, event.currentTarget.selectionEnd);
          onChange(block.type === "list" ? { ...block, items: event.target.value.split("\n") } : { ...block, text: event.target.value });
        }}
        placeholder={block.type === "heading" ? "Section heading" : block.type === "quote" ? "Quoted evidence" : block.type === "list" ? "One item per line" : "Write this section"}
      />
    </label>
  );
}

function convert(block: HubBlock, type: HubBlock["type"]): HubBlock {
  const text = block.type === "list" ? block.items.join("\n") : block.text;
  if (type === "list") return { id: block.id, type: "list", items: text.split("\n") };
  if (type === "image") return { id: block.id, type: "image", text, url: block.type === "video" || block.type === "image" ? block.url : "" };
  if (type === "video") return { id: block.id, type: "video", text, url: block.type === "image" || block.type === "video" ? block.url : "" };
  return { id: block.id, type, text };
}
