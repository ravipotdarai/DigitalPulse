import type { HubSeoCheck } from "../lib/hubSeo";

export function HubSeoHealth({
  score,
  intent,
  checks,
  aeo,
  notes,
  entitiesMentioned,
  entitiesTotal,
  live
}: {
  score: number;
  intent: string;
  checks: HubSeoCheck[];
  aeo: HubSeoCheck[];
  notes: string[];
  entitiesMentioned: number;
  entitiesTotal: number;
  live?: boolean;
}) {
  const attention = notes.filter(Boolean);
  return (
    <div className="seo-health">
      <p className="hero-kicker">SEO health</p>
      <div className="seo-health-score">
        <div>
          <p className="seo-health-label">SEO score</p>
          <p className="seo-health-number"><strong>{score}</strong> / 100</p>
        </div>
        <p className="ink-muted">{intent || "Unknown"} · {checks.filter((item) => item.passed).length}/{checks.length || 7} checks. Informational — not a ranking promise.</p>
      </div>
      <div className="seo-health-bar" aria-hidden="true">
        <span style={{ width: `${Math.max(0, Math.min(100, score))}%` }} />
      </div>
      <p className="ink-muted">{live ? "Live from the unsaved draft." : "Stored checklist from the last analyze."} Entity coverage {entitiesMentioned}/{entitiesTotal || 0} named records.</p>
      <ul className="seo-health-list">
        {checks.map((item) => (
          <li key={item.code} className={item.passed ? "is-ok" : "is-warn"}>
            <span>{item.passed ? "✓" : "⚠"}</span>
            <div>
              <strong>{item.label}</strong>
              <p>{item.note}</p>
            </div>
          </li>
        ))}
      </ul>
      <p className="hero-kicker">AEO</p>
      <ul className="seo-health-list is-aeo">
        {aeo.map((item) => (
          <li key={item.code} className={item.passed ? "is-ok" : "is-warn"}>
            <span>{item.passed ? "✓" : "⚠"}</span>
            <div>
              <strong>{item.label}</strong>
              <p>{item.note}</p>
            </div>
          </li>
        ))}
      </ul>
      {attention.length > 0 ? (
        <>
          <p className="hero-kicker">Needs attention</p>
          <ul className="seo-health-notes">{attention.map((note) => <li key={note}>{note}</li>)}</ul>
        </>
      ) : <p>Every stored SEO and AEO check passed.</p>}
    </div>
  );
}
