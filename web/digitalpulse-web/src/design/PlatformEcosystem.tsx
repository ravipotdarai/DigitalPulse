import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { useEffect, useId, useRef, useState, type CSSProperties, type PointerEvent } from "react";
import { BrandMark, brandColor } from "./BrandMark";
import { useMotionTiming } from "./motion";
import { LINK_LABEL, type LinkState } from "./platforms";

export type EcosystemNode = {
  code: string;
  name: string;
  category: string;
  state: LinkState;
  meta?: string | null;
  detail?: string | null;
};

const W = 1000;
const H = 540;
const CX = W / 2;
const CY = H / 2;
const PULSE_SECONDS: Partial<Record<LinkState, number>> = { live: 2.8, syncing: 1.1, warning: 4.2 };
const EASE: [number, number, number, number] = [0.16, 1, 0.3, 1];

function place(index: number, total: number, compact: boolean) {
  const dual = total > 8;
  const ring = dual ? index % 2 : 0;
  const slot = dual ? Math.floor(index / 2) : index;
  const count = dual ? Math.ceil(total / 2) : total;
  const angle = -Math.PI / 2 + (slot / count) * Math.PI * 2 + (ring ? Math.PI / count : 0);
  const rx = (compact ? 214 : 248) + ring * (compact ? 78 : 86);
  const ry = (compact ? 116 : 138) + ring * (compact ? 46 : 52);
  return { x: CX + Math.cos(angle) * rx, y: CY + Math.sin(angle) * ry };
}

function useStageTilt(reduce: boolean) {
  const stageRef = useRef<HTMLDivElement>(null);
  const target = useRef({ x: 0, y: 0 });
  const current = useRef({ x: 0, y: 0 });

  useEffect(() => {
    if (reduce) return;
    let frame = 0;
    const tick = () => {
      current.current.x += (target.current.x - current.current.x) * 0.08;
      current.current.y += (target.current.y - current.current.y) * 0.08;
      const node = stageRef.current;
      if (node) {
        node.style.setProperty("--mx", current.current.x.toFixed(4));
        node.style.setProperty("--my", current.current.y.toFixed(4));
      }
      frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [reduce]);

  function onPointerMove(event: PointerEvent<HTMLDivElement>) {
    if (reduce || !stageRef.current) return;
    const box = stageRef.current.getBoundingClientRect();
    target.current = {
      x: (event.clientX - box.left) / box.width - 0.5,
      y: (event.clientY - box.top) / box.height - 0.5
    };
  }

  function onPointerLeave() {
    target.current = { x: 0, y: 0 };
  }

  return { stageRef, onPointerMove, onPointerLeave };
}

/**
 * Business at the centre, platforms orbiting it. Link animation encodes state:
 * travelling pulses = authorized, fast pulses = authorizing, slow amber = needs attention, dashed = not connected.
 */
export function PlatformEcosystem({
  nodes,
  centerLabel,
  centerMeta,
  selected,
  onSelect,
  compact = false,
  caption,
  hint
}: {
  nodes: EcosystemNode[];
  centerLabel: string;
  centerMeta?: string;
  selected?: string | null;
  onSelect?: (code: string) => void;
  compact?: boolean;
  caption?: string;
  hint?: string;
}) {
  const reduce = useReducedMotion() ?? false;
  const { slow } = useMotionTiming();
  const { stageRef, onPointerMove, onPointerLeave } = useStageTilt(reduce);
  const [hovered, setHovered] = useState<string | null>(null);
  const [pinned, setPinned] = useState<string | null>(null);
  const glowId = `eco-core-${useId().replace(/:/g, "")}`;
  const placed = nodes.map((node, index) => ({ ...node, ...place(index, nodes.length, compact) }));
  const lit = hovered ?? selected ?? null;
  const inspectCode = hovered ?? selected ?? pinned;
  const inspected = placed.find((node) => node.code === inspectCode) ?? null;
  const inspectColor = inspected ? brandColor(inspected.code) : undefined;
  const tally = {
    live: nodes.filter((node) => node.state === "live" || node.state === "syncing").length,
    watch: nodes.filter((node) => node.state === "warning" || node.state === "failing").length,
    idle: nodes.filter((node) => node.state === "idle").length
  };

  function hover(code: string | null) {
    setHovered(code);
    if (code) setPinned(code);
  }

  return (
    <figure className={`eco${compact ? " is-compact" : ""}${lit ? " has-focus" : ""}`}>
      <ul className="eco-telemetry" aria-label="Channel states">
        <li className="is-live"><b>{tally.live}</b><span>Linked</span></li>
        <li className="is-watch"><b>{tally.watch}</b><span>Needs attention</span></li>
        <li className="is-idle"><b>{tally.idle}</b><span>Not connected</span></li>
      </ul>
      <div className="eco-stage" ref={stageRef} onPointerMove={onPointerMove} onPointerLeave={onPointerLeave}>
        <div className="eco-layer is-back" aria-hidden="true">
          <svg className="eco-links" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none">
            <defs>
              <radialGradient id={glowId} cx="50%" cy="38%" r="55%">
                <stop offset="0%" stopColor="#00F2FE" stopOpacity="0.38" />
                <stop offset="42%" stopColor="#4FACFE" stopOpacity="0.14" />
                <stop offset="100%" stopColor="#7F00FF" stopOpacity="0" />
              </radialGradient>
            </defs>
            <ellipse className="eco-glow" cx={CX} cy={CY} rx={compact ? 188 : 220} ry={compact ? 108 : 128} fill={`url(#${glowId})`} />
            <ellipse className="eco-orbit" cx={CX} cy={CY} rx={compact ? 292 : 334} ry={compact ? 162 : 190} />
            <ellipse className="eco-orbit is-inner" cx={CX} cy={CY} rx={compact ? 214 : 248} ry={compact ? 116 : 138} />
            {placed.map((node, index) => {
              const d = `M${CX} ${CY} L${node.x} ${node.y}`;
              const seconds = PULSE_SECONDS[node.state];
              const tone = node.state === "warning" ? "var(--dp-warning)" : node.state === "failing" ? "var(--dp-danger)" : brandColor(node.code);
              return (
                <g
                  key={node.code}
                  className={`eco-link is-${node.state}${lit === node.code ? " is-focus" : ""}`}
                  style={{ "--brand": tone, "--i": index } as CSSProperties}
                >
                  <path className="eco-link-base" d={d} />
                  {node.state !== "idle" ? <path className="eco-link-flow" d={d} /> : null}
                  {!reduce && seconds
                    ? [0, 0.5].map((offset) => (
                        <circle key={offset} className="eco-pulse" r={node.state === "syncing" ? 3 : 4}>
                          <animateMotion
                            dur={`${seconds}s`}
                            begin={`${offset * seconds}s`}
                            repeatCount="indefinite"
                            path={d}
                            keyPoints={node.state === "syncing" ? "0;1" : "1;0"}
                            keyTimes="0;1"
                            calcMode="linear"
                          />
                        </circle>
                      ))
                    : null}
                </g>
              );
            })}
          </svg>
        </div>

        <div className="eco-layer is-mid">
          <div className="eco-hub" style={lit && inspectColor ? ({ "--brand": inspectColor } as CSSProperties) : undefined}>
            <span className="eco-hub-glow" aria-hidden="true" />
            <span className="eco-hub-ring" aria-hidden="true" />
            <span className="eco-hub-ring is-late" aria-hidden="true" />
            <span className="eco-hub-core">
              <strong>{centerLabel}</strong>
              {centerMeta ? <small>{centerMeta}</small> : null}
            </span>
          </div>
        </div>

        <div className="eco-layer is-front">
          {placed.map((node, index) => {
            const style = {
              left: `${(node.x / W) * 100}%`,
              top: `${(node.y / H) * 100}%`,
              "--i": index,
              "--brand": brandColor(node.code)
            } as CSSProperties;
            const className = `eco-node is-${node.state}${selected === node.code ? " is-selected" : ""}${lit === node.code ? " is-focus" : ""}`;
            const inner = (
              <motion.span
                className="eco-node-inner"
                initial={reduce ? false : { opacity: 0, scale: 0.42 }}
                animate={{
                  opacity: 1,
                  scale: 1,
                  transition: { duration: reduce ? 0 : 0.72, delay: reduce ? 0 : 0.28 + index * 0.055, ease: EASE }
                }}
                whileHover={reduce ? undefined : { scale: 1.12, transition: { duration: 0.28, delay: 0, ease: EASE } }}
                whileTap={reduce || !onSelect ? undefined : { scale: 0.96, transition: { duration: 0.16, delay: 0 } }}
              >
                <BrandMark code={node.code} name={node.name} className={`is-${node.state}`} />
                <span className="eco-node-text">
                  <span className="eco-node-name">{node.name}</span>
                  <span className="eco-node-state">{node.meta ?? LINK_LABEL[node.state]}</span>
                </span>
                <i className="eco-node-pip" aria-hidden="true" />
              </motion.span>
            );
            const events = {
              onPointerEnter: () => hover(node.code),
              onPointerLeave: () => hover(null),
              onFocus: () => hover(node.code),
              onBlur: () => hover(null)
            };

            return onSelect ? (
              <button
                key={node.code}
                type="button"
                className={className}
                style={style}
                aria-pressed={selected === node.code}
                aria-label={`${node.name}: ${LINK_LABEL[node.state]}`}
                onClick={() => onSelect(node.code)}
                {...events}
              >
                {inner}
              </button>
            ) : (
              <div key={node.code} className={className} style={style} role="img" aria-label={`${node.name}: ${LINK_LABEL[node.state]}`} tabIndex={0} {...events}>
                {inner}
              </div>
            );
          })}
        </div>
      </div>

      <div className="eco-inspect" style={inspectColor ? ({ "--brand": inspectColor } as CSSProperties) : undefined}>
        <AnimatePresence mode="wait" initial={false}>
          {inspected ? (
            <motion.div
              key={inspected.code}
              className={`eco-inspect-card is-${inspected.state}`}
              role="status"
              initial={reduce ? false : { opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={reduce ? undefined : { opacity: 0, y: -8 }}
              transition={{ duration: reduce ? 0 : Math.max(0.28, slow * 0.5), ease: EASE }}
            >
              <span className="eco-inspect-head">
                <BrandMark code={inspected.code} name={inspected.name} className={`is-${inspected.state}`} />
                <span>
                  <strong>{inspected.name}</strong>
                  <small>
                    {inspected.category}
                    <i aria-hidden="true" />
                    {LINK_LABEL[inspected.state]}
                  </small>
                </span>
              </span>
              {inspected.detail ? <p>{inspected.detail}</p> : null}
              {hint ? <em>{hint}</em> : null}
            </motion.div>
          ) : (
            <motion.div
              key="idle"
              className="eco-inspect-idle"
              initial={reduce ? false : { opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={reduce ? undefined : { opacity: 0 }}
              transition={{ duration: reduce ? 0 : 0.28 }}
            >
              <strong>Hover a platform.</strong>
              <span>{hint ?? "Each mark is a channel this business lives on."}</span>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      <figcaption className="eco-legend">
        {(["live", "syncing", "warning", "failing", "idle"] as LinkState[]).map((state) => (
          <span key={state} className={`eco-key is-${state}`}>
            <i aria-hidden="true" />
            {LINK_LABEL[state]}
          </span>
        ))}
        {caption ? <span className="eco-caption">{caption}</span> : null}
      </figcaption>
    </figure>
  );
}
