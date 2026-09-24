import { severityRank, severityTone } from "./platforms";

/** Four ascending bars; filled count = severity rank. Readable without colour. */
export function SignalGlyph({ severity }: { severity: string }) {
  const rank = severityRank(severity);
  return (
    <span className={`signal-glyph is-${severityTone(severity)}`} role="img" aria-label={`${severity} severity`}>
      {[1, 2, 3, 4].map((bar) => (
        <i key={bar} className={bar <= rank ? "is-on" : undefined} style={{ height: `${bar * 25}%` }} />
      ))}
    </span>
  );
}

export function SignalRow({
  index,
  severity,
  title,
  meta,
  status,
  selected,
  onSelect
}: {
  index: number;
  severity: string;
  title: string;
  meta: string;
  status?: string;
  selected?: boolean;
  onSelect?: () => void;
}) {
  const body = (
    <>
      <span className="signal-index">{String(index).padStart(2, "0")}</span>
      <SignalGlyph severity={severity} />
      <span className="signal-text">
        <span className="signal-title">{title}</span>
        <span className="signal-meta">{meta}</span>
      </span>
      {status ? <span className={`signal-status is-${status.toLowerCase()}`}>{status}</span> : null}
    </>
  );
  if (!onSelect) return <div className="signal-row">{body}</div>;
  return (
    <button type="button" className={selected ? "signal-row is-selected" : "signal-row"} aria-pressed={selected} onClick={onSelect}>
      {body}
    </button>
  );
}
