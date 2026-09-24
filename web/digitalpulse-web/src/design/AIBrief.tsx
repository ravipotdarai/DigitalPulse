import { Sparkle20Regular } from "@fluentui/react-icons";
import type { ReactNode } from "react";

export type BriefStep = {
  label: "Observed" | "Why it matters" | "Evidence" | "Recommendation" | "Action" | "Result" | "Verify";
  value: ReactNode;
  tone?: "live" | "warning" | "failing" | "idle";
};

/**
 * DigitalPulse reasoning chain: observation → why → evidence → recommendation → action → result.
 * `provenance` must say where the reasoning came from; nothing here may claim model output it isn't.
 */
export function AIBrief({
  title,
  steps,
  provenance,
  footer
}: {
  title: string;
  steps: BriefStep[];
  provenance: string;
  footer?: ReactNode;
}) {
  return (
    <article className="brief">
      <header className="brief-head">
        <span className="brief-mark" aria-hidden="true"><Sparkle20Regular /></span>
        <div>
          <p className="brief-kicker">DigitalPulse brief</p>
          <h3 className="brief-title">{title}</h3>
        </div>
      </header>
      <ol className="brief-chain">
        {steps.map((step) => (
          <li key={step.label} className={step.tone ? `brief-step is-${step.tone}` : "brief-step"}>
            <span className="brief-label">{step.label}</span>
            <span className="brief-value">{step.value}</span>
          </li>
        ))}
      </ol>
      <p className="brief-provenance">{provenance}</p>
      {footer ? <div className="brief-foot">{footer}</div> : null}
    </article>
  );
}
