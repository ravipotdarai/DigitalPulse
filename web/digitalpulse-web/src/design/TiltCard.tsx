import { useReducedMotion } from "framer-motion";
import { useRef, type CSSProperties, type PointerEvent, type ReactNode } from "react";

export function TiltCard({
  children,
  className,
  as: Tag = "div"
}: {
  children: ReactNode;
  className?: string;
  as?: "div" | "section" | "article";
}) {
  const reduce = useReducedMotion() ?? false;
  const ref = useRef<HTMLElement | null>(null);

  const reset = () => {
    const node = ref.current;
    if (!node) return;
    node.style.setProperty("--tilt-x", "0deg");
    node.style.setProperty("--tilt-y", "0deg");
    node.style.setProperty("--spec-x", "50%");
    node.style.setProperty("--spec-y", "50%");
  };

  const onMove = (event: PointerEvent<HTMLElement>) => {
    if (reduce) return;
    const node = ref.current;
    if (!node) return;
    const box = node.getBoundingClientRect();
    const px = (event.clientX - box.left) / box.width - 0.5;
    const py = (event.clientY - box.top) / box.height - 0.5;
    node.style.setProperty("--tilt-x", `${(-py * 5).toFixed(2)}deg`);
    node.style.setProperty("--tilt-y", `${(px * 6).toFixed(2)}deg`);
    node.style.setProperty("--spec-x", `${(px + 0.5) * 100}%`);
    node.style.setProperty("--spec-y", `${(py + 0.5) * 100}%`);
  };

  return (
    <Tag
      ref={ref as never}
      className={className ? `tilt-card ${className}` : "tilt-card"}
      style={{ "--tilt-x": "0deg", "--tilt-y": "0deg", "--spec-x": "50%", "--spec-y": "50%" } as CSSProperties}
      onPointerMove={onMove}
      onPointerLeave={reset}
    >
      {children}
    </Tag>
  );
}
