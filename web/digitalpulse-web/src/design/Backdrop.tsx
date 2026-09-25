import { useReducedMotion } from "framer-motion";
import { useEffect, useRef } from "react";

/** Ambient void mesh. Decorative only — never a data source. */
export function Backdrop() {
  const reduce = useReducedMotion() ?? false;
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || reduce) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const pointer = { x: 0.5, y: 0.4, tx: 0.5, ty: 0.4 };
    let frame = 0;
    let running = true;

    const resize = () => {
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(window.innerWidth * ratio);
      canvas.height = Math.floor(window.innerHeight * ratio);
      canvas.style.width = `${window.innerWidth}px`;
      canvas.style.height = `${window.innerHeight}px`;
      ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    };

    const onMove = (event: PointerEvent) => {
      pointer.tx = event.clientX / window.innerWidth;
      pointer.ty = event.clientY / window.innerHeight;
    };

    const draw = (now: number) => {
      if (!running) return;
      pointer.x += (pointer.tx - pointer.x) * 0.045;
      pointer.y += (pointer.ty - pointer.y) * 0.045;
      const w = window.innerWidth;
      const h = window.innerHeight;
      ctx.clearRect(0, 0, w, h);
      const t = now / 4000;
      for (let i = 0; i < 28; i++) {
        const angle = t + i * 0.42;
        const x = w * (0.18 + pointer.x * 0.55 + Math.cos(angle) * 0.16);
        const y = h * (0.22 + pointer.y * 0.45 + Math.sin(angle * 0.8) * 0.14);
        const radius = 90 + (i % 7) * 28;
        const cyan = i % 2 === 0;
        const gradient = ctx.createRadialGradient(x, y, 0, x, y, radius);
        gradient.addColorStop(0, cyan ? "rgba(0, 242, 254, 0.085)" : "rgba(127, 0, 255, 0.07)");
        gradient.addColorStop(1, "rgba(5, 6, 10, 0)");
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.fill();
      }
      frame = requestAnimationFrame(draw);
    };

    resize();
    window.addEventListener("resize", resize);
    window.addEventListener("pointermove", onMove, { passive: true });
    frame = requestAnimationFrame(draw);
    return () => {
      running = false;
      cancelAnimationFrame(frame);
      window.removeEventListener("resize", resize);
      window.removeEventListener("pointermove", onMove);
    };
  }, [reduce]);

  return (
    <div className="backdrop" aria-hidden="true">
      <canvas ref={canvasRef} className="backdrop-mesh" />
      <span className="backdrop-orb is-a" />
      <span className="backdrop-orb is-b" />
      <span className="backdrop-orb is-c" />
      <span className="backdrop-grid" />
    </div>
  );
}
