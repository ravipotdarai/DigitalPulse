import type { CSSProperties } from "react";

export const CORE_LOOP = ["Connect", "Understand", "Detect", "Create", "Approve", "Execute", "Verify", "Monitor"] as const;
export type LoopStage = (typeof CORE_LOOP)[number];
export type StageState = "done" | "now" | "held" | "next";

/**
 * The DigitalPulse core loop as a progress track. `states` comes from real data; stages that
 * cannot run yet (for example Execute without live provider writes) should be "held", not "done".
 */
export function LoopTrack({
  states,
  notes,
  compact = false
}: {
  states: Record<LoopStage, StageState>;
  notes?: Partial<Record<LoopStage, string>>;
  compact?: boolean;
}) {
  const doneCount = CORE_LOOP.filter((stage) => states[stage] === "done").length;
  return (
    <ol className={compact ? "loop-track is-compact" : "loop-track"} aria-label={`Core loop: ${doneCount} of ${CORE_LOOP.length} stages complete`}>
      <span className="loop-track-rail" aria-hidden="true">
        <span className="loop-track-fill" style={{ width: `${(doneCount / (CORE_LOOP.length - 1)) * 100}%` }} />
      </span>
      {CORE_LOOP.map((stage, index) => (
        <li key={stage} className={`loop-stage is-${states[stage]}`} style={{ "--i": index } as CSSProperties}>
          <span className="loop-dot" aria-hidden="true" />
          <span className="loop-num">{String(index + 1).padStart(2, "0")}</span>
          <span className="loop-name">{stage}</span>
          {!compact && notes?.[stage] ? <span className="loop-note">{notes[stage]}</span> : null}
          <span className="sr-only">{states[stage]}</span>
        </li>
      ))}
    </ol>
  );
}
