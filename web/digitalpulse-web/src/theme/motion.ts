import { useEffect, useState } from "react";

export function prefersReducedMotion() {
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

export function useParallax(strength = 18) {
  const [offset, setOffset] = useState(0);

  useEffect(() => {
    if (prefersReducedMotion() || strength <= 0) {
      setOffset(0);
      return;
    }

    const onScroll = () => setOffset(window.scrollY * (strength / 1000));
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [strength]);

  return offset;
}
