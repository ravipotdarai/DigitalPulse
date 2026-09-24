import { Stagger, StaggerItem } from "./motion";

export function OnboardingFrame({
  title,
  step,
  total = 5,
  lead,
  children
}: {
  title: string;
  step: number;
  total?: number;
  lead?: string;
  children: React.ReactNode;
}) {
  return (
    <main className="wizard">
      <Stagger>
        <StaggerItem>
          <p className="chapter-num">{String(step).padStart(2, "0")}</p>
        </StaggerItem>
        <StaggerItem>
          <div className="wizard-steps" aria-hidden="true">
            {Array.from({ length: total }, (_, index) => (
              <i key={index} className={index < step ? "is-on" : undefined} />
            ))}
          </div>
        </StaggerItem>
        <StaggerItem>
          <p className="hero-kicker">Onboarding {step} / {total}</p>
          <h1 className="display display-page">{title}</h1>
          {lead ? <p className="lead">{lead}</p> : null}
        </StaggerItem>
        <StaggerItem>{children}</StaggerItem>
      </Stagger>
    </main>
  );
}

export function Wizard({ title, step, children }: { title: string; step: string; children: React.ReactNode }) {
  const current = Number(step.split("/")[0]) || 1;
  return <OnboardingFrame title={title} step={current}>{children}</OnboardingFrame>;
}
