import { useState } from "react";
import { ApiError, api, type HubAssist } from "../lib/api";
import { Button } from "../design/Button";
import { AreaField, SelectField } from "../design/Field";

const ACTIONS = [
  ["outline", "Generate outline"],
  ["draft", "Generate draft"],
  ["rewrite", "Rewrite"],
  ["shorten", "Shorten"],
  ["expand", "Expand"],
  ["tone", "Change tone"],
  ["faq", "Create FAQ"],
  ["meta-title", "Generate meta title"],
  ["meta-description", "Generate meta description"],
  ["social", "Generate social post"],
  ["linkedin", "Generate LinkedIn version"],
  ["google", "Generate Google version"],
  ["instagram", "Generate Instagram caption"],
  ["youtube", "Create YouTube script"]
];

export function HubAssist({
  businessId,
  contentId,
  section,
  onAccept
}: {
  businessId: string;
  contentId: string | null;
  section: string;
  onAccept: (suggestion: HubAssist) => void;
}) {
  const [action, setAction] = useState("outline");
  const [instruction, setInstruction] = useState("");
  const [preview, setPreview] = useState<HubAssist | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function generate() {
    setBusy(true);
    setError(null);
    try {
      const result = await api.assistHubContent(businessId, {
        action,
        instruction: instruction.trim() || null,
        contentId,
        section
      });
      setPreview(result);
    } catch (err) {
      setPreview(null);
      setError(err instanceof ApiError ? err.title : "The assistant could not build a preview.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="hub-assist">
      <p className="hero-kicker">AI assistant</p>
      <p className="ink-muted">GENERATE builds a preview from approved facts. ACCEPT replaces the article for outline, draft, rewrite, shorten, expand, tone, and FAQ. Social, LinkedIn, Google, Instagram, and YouTube append a section. REJECT drops the preview. Nothing is saved until you save the article.</p>
      <SelectField label="Action" value={action} onChange={setAction} options={ACTIONS.map(([value, label]) => ({ value, label }))} />
      <AreaField label="Instruction" value={instruction} onChange={setInstruction} />
      <div className="row-actions">
        <Button appearance="primary" disabled={busy} onClick={() => void generate()}>Generate</Button>
        <Button appearance="subtle" disabled={!preview} onClick={() => { if (preview) onAccept(preview); setPreview(null); }}>Accept</Button>
        <Button appearance="subtle" disabled={!preview} onClick={() => setPreview(null)}>Reject</Button>
      </div>
      {error ? <p className="note-err" role="alert">{error}</p> : null}
      {preview ? (
        <article className="hub-assist-preview">
          <p className="ink-muted">{preview.providerName} · {preview.isLive ? "Live model" : "Development assemble"} · applies to {preview.target}</p>
          <p>{preview.hold}</p>
          <pre>{preview.suggestion}</pre>
        </article>
      ) : <p className="ink-muted">No preview. Generate first.</p>}
    </div>
  );
}
