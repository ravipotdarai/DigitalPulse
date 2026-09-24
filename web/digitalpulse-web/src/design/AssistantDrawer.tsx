import { Dismiss20Regular } from "@fluentui/react-icons";
import { useUi } from "../state/ui";
import { AIBrief } from "./AIBrief";
import { DrawerFrame } from "./motion";

export function AssistantDrawer({ insights }: { insights: { title: string; why: string; action?: string }[] }) {
  const open = useUi((s) => s.assistantOpen);
  const setAssistant = useUi((s) => s.setAssistant);
  return (
    <DrawerFrame open={open} label="DigitalPulse brief">
      <div className="drawer-head">
        <p className="hero-kicker kicker-flush">DigitalPulse brief</p>
        <button type="button" className="icon-btn" aria-label="Close brief" onClick={() => setAssistant(false)}>
          <Dismiss20Regular />
        </button>
      </div>
      <h2 className="page-title drawer-title">What the record shows</h2>
      {insights.length === 0 ? (
        <p className="empty-line">
          <strong>The identity record is complete enough</strong>
          Run DigitalPulse Check for evidence from the website and authorized platforms.
        </p>
      ) : (
        insights.map((item) => (
          <AIBrief
            key={item.title}
            title={item.title}
            steps={[
              { label: "Observed", value: item.title, tone: "warning" },
              { label: "Why it matters", value: item.why },
              ...(item.action ? [{ label: "Recommendation" as const, value: item.action }] : [])
            ]}
            provenance="Derived from the business record. No language model generated this text."
          />
        ))
      )}
    </DrawerFrame>
  );
}
