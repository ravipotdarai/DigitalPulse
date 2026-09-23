import { Button } from "@fluentui/react-components";
import { useUi } from "../state/ui";

export function AssistantDrawer({ insights }: { insights: { title: string; why: string; action?: string }[] }) {
  const open = useUi((s) => s.assistantOpen);
  const setAssistant = useUi((s) => s.setAssistant);
  if (!open) return null;
  return (
    <aside className="drawer" aria-label="AI insights">
      <div className="flex items-center justify-between gap-3">
        <p className="hero-kicker" style={{ margin: 0 }}>Insights</p>
        <Button appearance="subtle" onClick={() => setAssistant(false)}>Close</Button>
      </div>
      <h2 className="display" style={{ fontSize: "1.8rem", margin: 0 }}>What the record shows</h2>
      <p style={{ color: "var(--muted)", margin: 0 }}>
        These are generated from the current business record. They are not model output and they do not claim platform access.
      </p>
      {insights.length === 0 ? (
        <div className="dp-empty"><strong>Identity is complete enough</strong><p>Add platforms when Connection Center ships.</p></div>
      ) : insights.map((item) => (
        <article key={item.title} className="panel">
          <h3>{item.title}</h3>
          <p style={{ color: "var(--muted)", margin: 0 }}>{item.why}</p>
          {item.action ? <p className="sev sev-warn" style={{ marginTop: "0.6rem" }}>{item.action}</p> : null}
        </article>
      ))}
    </aside>
  );
}
