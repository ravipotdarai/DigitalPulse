export function HealthRing({ value, label }: { value: number; label?: string }) {
  const clamped = Math.max(0, Math.min(100, value));
  const radius = 42;
  const circ = 2 * Math.PI * radius;
  const dash = (clamped / 100) * circ;
  return (
    <div className="health-ring" aria-label={label ?? `Health ${clamped} percent`}>
      <svg viewBox="0 0 100 100" role="img">
        <circle cx="50" cy="50" r={radius} fill="none" stroke="var(--line-strong)" strokeWidth="6" />
        <circle
          cx="50"
          cy="50"
          r={radius}
          fill="none"
          stroke="var(--signal)"
          strokeWidth="6"
          strokeDasharray={`${dash} ${circ}`}
          strokeLinecap="square"
        />
        <text x="50" y="56" textAnchor="middle">{clamped}</text>
      </svg>
    </div>
  );
}
