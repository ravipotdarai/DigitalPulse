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
      <div className="wizard-steps" aria-hidden="true">
        {Array.from({ length: total }, (_, index) => (
          <i key={index} className={index < step ? "is-on" : undefined} />
        ))}
      </div>
      <p className="hero-kicker">Onboarding {step} / {total}</p>
      <h1 className="display">{title}</h1>
      {lead ? <p className="lead">{lead}</p> : null}
      {children}
    </main>
  );
}

export function Wizard({ title, step, children }: { title: string; step: string; children: React.ReactNode }) {
  const current = Number(step.split("/")[0]) || 1;
  return <OnboardingFrame title={title} step={current}>{children}</OnboardingFrame>;
}
