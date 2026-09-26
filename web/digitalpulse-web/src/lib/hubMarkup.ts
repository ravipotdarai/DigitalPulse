export type HubBlock =
  | { id: string; type: "paragraph" | "heading" | "quote"; text: string }
  | { id: string; type: "list"; items: string[] }
  | { id: string; type: "image" | "video"; text: string; url: string };

export type HubInline = { text: string; href?: string; bold?: boolean; italic?: boolean };

const IMAGE = /^!\[(.*)\]\((.+)\)$/;
const VIDEO = /^!video\[(.*)\]\((.+)\)$/i;
const HUB = /^\/hub\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\/[a-z0-9-]+$/;
const HUB_MEDIA = /^\/v1\/hub\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\/media\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const WORKSPACE_MEDIA = /^\/v1\/businesses\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\/content\/media\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const INLINE = /\[([^\]]+)\]\(([^)]+)\)|\*\*(.+?)\*\*|\*(.+?)\*/g;
const YOUTUBE_ID = /^[A-Za-z0-9_-]{11}$/;
const VIMEO_ID = /^\d{6,12}$/;

let stamp = 0;
export function hubBlockId() {
  stamp += 1;
  return `hub-block-${stamp}-${Math.random().toString(36).slice(2, 8)}`;
}

export function isSafeHref(url: string) {
  const value = url.trim();
  if (value.startsWith("/hub/")) return HUB.test(value);
  if (value.startsWith("/v1/hub/")) return HUB_MEDIA.test(value);
  if (value.startsWith("/v1/businesses/")) return WORKSPACE_MEDIA.test(value);
  try {
    const parsed = new URL(value);
    if (parsed.protocol !== "https:") return false;
    const host = parsed.hostname.toLowerCase().replace(/\.$/, "");
    if (host === "localhost" || host.endsWith(".local") || host.endsWith(".internal") || host.endsWith(".localhost")) return false;
    if (/^(127|10)\./.test(host) || /^192\.168\./.test(host) || /^172\.(1[6-9]|2\d|3[0-1])\./.test(host) || /^169\.254\./.test(host)) return false;
    return true;
  } catch {
    return false;
  }
}

export function videoEmbed(url: string): { src: string; provider: string } | null {
  if (!isSafeHref(url)) return null;
  try {
    const parsed = new URL(url.trim());
    const host = parsed.hostname.toLowerCase().replace(/\.$/, "");
    if (host === "youtu.be" || host === "www.youtu.be") {
      const id = parsed.pathname.replace(/^\//, "");
      return YOUTUBE_ID.test(id) ? { src: `https://www.youtube-nocookie.com/embed/${id}`, provider: "YouTube" } : null;
    }
    if (host.endsWith("youtube.com") || host.endsWith("youtube-nocookie.com")) {
      const id = parsed.searchParams.get("v") || parsed.pathname.split("/embed/")[1]?.split("/")[0] || "";
      return YOUTUBE_ID.test(id) ? { src: `https://www.youtube-nocookie.com/embed/${id}`, provider: "YouTube" } : null;
    }
    if (host.endsWith("vimeo.com")) {
      const id = parsed.pathname.split("/").filter(Boolean).at(-1) ?? "";
      return VIMEO_ID.test(id) ? { src: `https://player.vimeo.com/video/${id}`, provider: "Vimeo" } : null;
    }
  } catch {
    return null;
  }
  return null;
}

export function parseHubMarkup(body: string): HubBlock[] {
  if (!body.trim()) return [];
  return body.replace(/\r\n/g, "\n").trim().split(/\n{2,}/).map(parseChunk).filter((block) => {
    if (block.type === "list") return block.items.some((item) => item.trim());
    if (block.type === "image" || block.type === "video") return Boolean(block.url.trim());
    return Boolean(block.text.trim());
  });
}

export function serializeHubMarkup(blocks: HubBlock[]) {
  return blocks.map(serializeChunk).filter((part) => part.trim()).join("\n\n");
}

export function splitInlines(text: string): HubInline[] {
  const parts: HubInline[] = [];
  let cursor = 0;
  INLINE.lastIndex = 0;
  let match: RegExpExecArray | null;
  while ((match = INLINE.exec(text))) {
    if (match.index > cursor) parts.push({ text: text.slice(cursor, match.index) });
    if (match[1] != null) {
      const href = match[2].trim();
      parts.push(isSafeHref(href) ? { text: match[1], href } : { text: match[1] });
    } else if (match[3] != null) {
      parts.push({ text: match[3], bold: true });
    } else {
      parts.push({ text: match[4], italic: true });
    }
    cursor = match.index + match[0].length;
  }
  if (cursor < text.length) parts.push({ text: text.slice(cursor) });
  return parts.length ? parts : [{ text }];
}

export function wrapSelection(value: string, start: number, end: number, before: string, after: string) {
  const selected = value.slice(start, end) || "text";
  return {
    text: `${value.slice(0, start)}${before}${selected}${after}${value.slice(end)}`,
    cursor: start + before.length + selected.length + after.length
  };
}

export function ensureHubBlocks(blocks: HubBlock[]): HubBlock[] {
  return blocks.length ? blocks : [{ id: hubBlockId(), type: "paragraph", text: "" }];
}

function parseChunk(chunk: string): HubBlock {
  const lines = chunk.split("\n").map((line) => line.trimEnd()).filter((line) => line.length > 0);
  const first = lines[0] ?? "";
  const video = first.match(VIDEO);
  if (lines.length === 1 && video) return { id: hubBlockId(), type: "video", text: video[1], url: video[2].trim() };
  const image = first.match(IMAGE);
  if (lines.length === 1 && image) return { id: hubBlockId(), type: "image", text: image[1], url: image[2].trim() };
  if (lines.length > 0 && lines.every((line) => line.startsWith("> "))) {
    return { id: hubBlockId(), type: "quote", text: lines.map((line) => line.slice(2)).join("\n") };
  }
  if (lines.length > 0 && lines.every((line) => line.startsWith("- "))) {
    return { id: hubBlockId(), type: "list", items: lines.map((line) => line.slice(2)) };
  }
  if (first.startsWith("## ")) return { id: hubBlockId(), type: "heading", text: first.slice(3) };
  if (first.startsWith("# ")) return { id: hubBlockId(), type: "heading", text: first.slice(2) };
  return { id: hubBlockId(), type: "paragraph", text: lines.join("\n") };
}

function serializeChunk(block: HubBlock) {
  if (block.type === "heading") return `# ${block.text.trim()}`;
  if (block.type === "quote") return block.text.split("\n").map((line) => `> ${line.trimEnd()}`).join("\n");
  if (block.type === "list") return block.items.map((item) => `- ${item.trim()}`).filter((item) => item.length > 2).join("\n");
  if (block.type === "image") return `![${block.text.trim()}](${block.url.trim()})`;
  if (block.type === "video") return `!video[${block.text.trim()}](${block.url.trim()})`;
  return block.text.trim();
}
