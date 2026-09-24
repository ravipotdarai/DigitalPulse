import type { ReactNode } from "react";
import { Reveal } from "./motion";

export function PageHeader({
  kicker,
  title,
  lead,
  actions,
  aside,
  children
}: {
  kicker: string;
  title: ReactNode;
  lead?: ReactNode;
  actions?: ReactNode;
  aside?: ReactNode;
  children?: ReactNode;
}) {
  return (
    <Reveal as="header" className={aside ? "page-head has-aside" : "page-head"}>
      <div className="page-head-main">
        <p className="hero-kicker">{kicker}</p>
        <h1 className="page-title">{title}</h1>
        {lead ? <p className="page-lead">{lead}</p> : null}
        {actions ? <div className="page-actions">{actions}</div> : null}
        {children}
      </div>
      {aside ? <div className="page-head-aside">{aside}</div> : null}
    </Reveal>
  );
}

export function Meter({ label, value, max, detail }: { label: string; value: number; max: number; detail?: string }) {
  const ratio = max > 0 ? Math.min(1, value / max) : 0;
  const tone = ratio >= 1 ? "failing" : ratio >= 0.8 ? "warning" : "live";
  return (
    <div className={`meter is-${tone}`}>
      <div className="meter-head">
        <span>{label}</span>
        <strong>{value}<small> / {max}</small></strong>
      </div>
      <span
        className="meter-bar"
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={max}
        aria-valuenow={value}
      >
        <i style={{ width: `${ratio * 100}%` }} />
      </span>
      {detail ? <p className="meter-detail">{detail}</p> : null}
    </div>
  );
}

export function SectionTitle({ kicker, title, action }: { kicker?: string; title: string; action?: ReactNode }) {
  return (
    <div className="section-title">
      <div>
        {kicker ? <p className="section-kicker">{kicker}</p> : null}
        <h2>{title}</h2>
      </div>
      {action ? <div className="section-action">{action}</div> : null}
    </div>
  );
}
