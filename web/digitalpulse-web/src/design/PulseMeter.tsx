const BEAT = "l18 0 6 -10 7 22 7 -34 7 22 5 0";

function tone(score: number) {
  if (score >= 75) return "live";
  if (score >= 45) return "warning";
  return "failing";
}

/**
 * Digital presence health. The ring shows coverage; the trace beats harder the healthier
 * the record is, so a thin record literally reads as a weak pulse.
 */
export function PulseMeter({ score, label, caption }: { score: number; label: string; caption?: string }) {
  const value = Math.max(0, Math.min(100, Math.round(score)));
  const radius = 88;
  const circ = 2 * Math.PI * radius;
  const amplitude = 0.35 + (value / 100) * 0.65;
  const beats = Array.from({ length: 5 }, () => BEAT).join(" l14 0 ");

  return (
    <figure className={`pulse-meter is-${tone(value)}`} aria-label={`${label}: ${value} out of 100`}>
      <div className="pulse-meter-ring">
        <svg viewBox="0 0 200 200" aria-hidden="true">
          <circle className="pulse-meter-track" cx="100" cy="100" r={radius} />
          <circle
            className="pulse-meter-arc"
            cx="100"
            cy="100"
            r={radius}
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
          <strong>{value}</strong>
          <span>{label}</span>
        </div>
      </div>
      <svg className="pulse-meter-trace" viewBox="0 0 320 60" preserveAspectRatio="none" aria-hidden="true">
        <line className="pulse-meter-baseline" x1="0" y1="30" x2="320" y2="30" />
        <g transform={`translate(0 30) scale(1 ${amplitude}) translate(0 -30)`}>
          <path className="pulse-meter-beat" d={`M0 30 ${beats} L320 30`} pathLength={1} />
        </g>
      </svg>
      {caption ? <figcaption>{caption}</figcaption> : null}
    </figure>
  );
}
