import type { ReactNode } from "react";
import { BrandMark } from "./BrandMark";

export function ChannelCard({
  code,
  name,
  category,
  rows,
  footer,
  children
}: {
  code: string;
  name: string;
  category?: string;
  rows?: { label: string; value: string; tone?: "ok" | "hold" | "warn" }[];
  footer?: ReactNode;
  children?: ReactNode;
}) {
  return (
    <article className="channel-card">
      <header className="channel-card-head">
        <BrandMark code={code} name={name} />
        <div>
          {category ? <p className="hero-kicker">{category}</p> : null}
          <h3>{name}</h3>
        </div>
      </header>
      {rows?.map((row) => (
        <div className="row-line" key={row.label}>
          <span>{row.label}</span>
          <span className={row.tone ? `sev sev-${row.tone}` : undefined}>{row.value}</span>
        </div>
      ))}
      {children}
      {footer ? <div className="channel-card-foot">{footer}</div> : null}
    </article>
  );
}

export function ChannelChip({
  code,
  name,
  selected,
  onSelect
}: {
  code: string;
  name: string;
  selected?: boolean;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      className={selected ? "channel-chip is-on" : "channel-chip"}
      aria-pressed={selected}
      onClick={onSelect}
    >
      <BrandMark code={code} name={name} />
      <span>{name}</span>
    </button>
  );
}
