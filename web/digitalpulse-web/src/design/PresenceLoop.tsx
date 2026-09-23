import { useState } from "react";

export const LOOP = [
  { id: "business", label: "Business" },
  { id: "identity", label: "Digital identity" },
  { id: "platforms", label: "Google / Meta / LinkedIn / YouTube" },
  { id: "analysis", label: "AI analysis" },
  { id: "findings", label: "Findings" },
  { id: "content", label: "Content" },
  { id: "approval", label: "Approval" },
  { id: "execution", label: "Execution" },
  { id: "verify", label: "Verification" },
  { id: "monitor", label: "Monitoring" }
] as const;

export function PresenceLoop({ active = "identity" }: { active?: string }) {
  const [hover, setHover] = useState<string | null>(null);
  const current = hover ?? active;
  return (
    <ol className="loop">
      {LOOP.map((node, index) => (
        <li
          key={node.id}
          className={node.id === current || index <= LOOP.findIndex((item) => item.id === active) ? "loop-node is-live" : "loop-node"}
          onMouseEnter={() => setHover(node.id)}
          onMouseLeave={() => setHover(null)}
        >
          <b />
          <span>{node.label}</span>
          <small>{String(index + 1).padStart(2, "0")}</small>
        </li>
      ))}
    </ol>
  );
}

export function PresenceTimeline({ now }: { now: string }) {
  const index = LOOP.findIndex((item) => item.id === now);
  return (
    <ol className="timeline">
      {LOOP.map((node, i) => (
        <li key={node.id} className={i < index ? "is-done" : i === index ? "is-now" : undefined}>
          {node.label}
        </li>
      ))}
    </ol>
  );
}
