import { useId } from "react";

const BEAT = "l18 0 6 -10 7 22 7 -34 7 22 5 0";

function tone(score: number) {
  if (score >= 75) return "live";
  if (score >= 45) return "warning";
  return "failing";
}

function sweep(kind: ReturnType<typeof tone>) {
  if (kind === "warning") return ["#FFB800", "#FF8A00"] as const;
  if (kind === "failing") return ["#FF6B6B", "#FF3B30"] as const;
  return ["#00F2FE", "#4FACFE"] as const;
}

/**
 * Digital presence health. The ring shows coverage; the trace beats harder the healthier
 * the record is, so a thin record literally reads as a weak pulse.
 */
export function PulseMeter({
  score,
  label,
  caption,
  facets
}: {
  score: number;
  label: string;
  caption?: string;
  facets?: { label: string; ok: boolean }[];
}) {
  const uid = useId().replace(/:/g, "");
  const value = Math.max(0, Math.min(100, Math.round(score)));
  const kind = tone(value);
  const [from, to] = sweep(kind);
  const radius = 88;
  const circ = 2 * Math.PI * radius;
  const amplitude = 0.35 + (value / 100) * 0.65;
  const beats = Array.from({ length: 5 }, () => BEAT).join(" l14 0 ");
  const grad = `pulseSweep-${uid}`;
  const glow = `pulseGlow-${uid}`;

  return (
    <figure className={`pulse-meter is-${kind}`} aria-label={`${label}: ${value} out of 100`}>
      <div className="pulse-meter-ring">
        <svg viewBox="0 0 200 200" aria-hidden="true">
          <defs>
            <linearGradient id={grad} x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor={from} />
              <stop offset="100%" stopColor={to} />
            </linearGradient>
            <filter id={glow} x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="2.6" result="blur" />
              <feMerge>
                <feMergeNode in="blur" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
          </defs>
          <circle className="pulse-meter-halo" cx="100" cy="100" r={radius} />
          <circle className="pulse-meter-track" cx="100" cy="100" r={radius} />
          <circle
            className="pulse-meter-arc"
            cx="100"
            cy="100"
            r={radius}
            stroke={`url(#${grad})`}
            filter={`url(#${glow})`}
            strokeDasharray={`${(value / 100) * circ} ${circ}`}
          />
          {Array.from({ length: 40 }, (_, i) => {
            const angle = (i / 40) * Math.PI * 2;
            const inner = 74;
            const outer = i % 5 === 0 ? 67 : 70;
            return (
              <line
                key={i}
                className="pulse-meter-tick"
                x1={100 + Math.cos(angle) * inner}
                y1={100 + Math.sin(angle) * inner}
                x2={100 + Math.cos(angle) * outer}
                y2={100 + Math.sin(angle) * outer}
              />
            );
          })}
        </svg>
        <div className="pulse-meter-value">
          <strong>{value}<em>/100</em></strong>
          <span>{label}</span>
        </div>
      </div>
      <svg className="pulse-meter-trace" viewBox="0 0 320 60" preserveAspectRatio="none" aria-hidden="true">
        <line className="pulse-meter-baseline" x1="0" y1="30" x2="320" y2="30" />
        <g transform={`translate(0 30) scale(1 ${amplitude}) translate(0 -30)`}>
          <path className="pulse-meter-beat" d={`M0 30 ${beats} L320 30`} pathLength={1} />
        </g>
      </svg>
      {facets?.length ? (
        <ul className="pulse-meter-facets">
          {facets.map((facet) => (
            <li key={facet.label} className={facet.ok ? "is-live" : "is-idle"}>
              <i aria-hidden="true" />
              {facet.label}
            </li>
          ))}
        </ul>
      ) : null}
      {caption ? <figcaption>{caption}</figcaption> : null}
    </figure>
  );
}
