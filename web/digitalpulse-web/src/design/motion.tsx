import { AnimatePresence, motion, useReducedMotion, type HTMLMotionProps, type Variants } from "framer-motion";
import type { ReactNode } from "react";
import { createPortal } from "react-dom";
import { useTheme } from "../theme";

/** DigitalPulse motion language. Pages and chrome consume these tokens — do not invent local timings. */
export const MOTION = {
  ease: [0.16, 1, 0.3, 1] as const,
  easeOut: [0.22, 1, 0.36, 1] as const,
  duration: {
    instant: 0.12,
    fast: 0.18,
    base: 0.28,
    enter: 0.48,
    slow: 0.7
  },
  distance: { xs: 6, sm: 10, md: 14, lg: 22 },
  stagger: { tight: 0.035, base: 0.055, loose: 0.09 },
  scale: { enter: 0.985, press: 0.98 }
};

function seconds(value: string) {
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed / 1000 : MOTION.duration.base;
}

export function useMotionTiming() {
  const { tokens } = useTheme();
  const reduce = useReducedMotion() ?? false;
  const themeBase = seconds(tokens.motion.duration);
  const themeSlow = seconds(tokens.motion.durationSlow);
  return {
    reduce,
    instant: reduce ? 0 : MOTION.duration.instant,
    fast: reduce ? 0 : MOTION.duration.fast,
    base: reduce ? 0 : themeBase,
    enter: reduce ? 0 : MOTION.duration.enter,
    slow: reduce ? 0 : themeSlow,
    ease: MOTION.ease,
    easeOut: MOTION.easeOut,
    stagger: reduce ? 0 : MOTION.stagger.base,
    distance: MOTION.distance
  };
}

export function pageTransition(reduce: boolean) {
  return {
    initial: reduce ? false : { opacity: 0 },
    animate: { opacity: 1 },
    exit: reduce ? undefined : { opacity: 0 },
    transition: {
      duration: reduce ? 0 : MOTION.duration.fast,
      ease: MOTION.ease
    }
  };
}

export function panelTransition(reduce: boolean, duration: number) {
  return {
    initial: reduce ? false : { opacity: 0, x: MOTION.distance.md },
    animate: { opacity: 1, x: 0 },
    exit: reduce ? undefined : { opacity: 0, x: -MOTION.distance.xs },
    transition: { duration, ease: MOTION.ease }
  };
}

export function PageEnter({ children, className }: { children: ReactNode; className?: string }) {
  const { reduce, ease } = useMotionTiming();
  return (
    <motion.div
      className={className}
      initial={reduce ? false : { opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: reduce ? 0 : 0.22, ease }}
    >
      {children}
    </motion.div>
  );
}

export function Reveal({
  children,
  delay = 0,
  className,
  as = "div"
}: {
  children: ReactNode;
  delay?: number;
  className?: string;
  as?: "div" | "section" | "article" | "header" | "aside";
}) {
  const { reduce, enter, ease } = useMotionTiming();
  const Tag = motion[as];
  return (
    <Tag
      className={className}
      initial={reduce ? false : { opacity: 0, y: MOTION.distance.md }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{ duration: enter, delay: reduce ? 0 : delay, ease }}
    >
      {children}
    </Tag>
  );
}

export function Stagger({
  children,
  className,
  as = "div",
  gap = MOTION.stagger.base
}: {
  children: ReactNode;
  className?: string;
  as?: "div" | "ol" | "ul" | "section";
  gap?: number;
}) {
  const { reduce } = useMotionTiming();
  const Tag = motion[as];
  const variants: Variants = {
    hidden: {},
    shown: { transition: { staggerChildren: reduce ? 0 : gap, delayChildren: reduce ? 0 : 0.04 } }
  };
  return (
    <Tag className={className} variants={variants} initial={reduce ? false : "hidden"} animate="shown">
      {children}
    </Tag>
  );
}

export function StaggerItem({
  children,
  className,
  as = "div"
}: {
  children: ReactNode;
  className?: string;
  as?: "div" | "li" | "article";
}) {
  const { reduce, enter, ease } = useMotionTiming();
  const Tag = motion[as];
  const variants: Variants = {
    hidden: reduce ? { opacity: 1, y: 0 } : { opacity: 0, y: MOTION.distance.sm },
    shown: { opacity: 1, y: 0, transition: { duration: reduce ? 0 : enter, ease } }
  };
  return (
    <Tag className={className} variants={variants}>
      {children}
    </Tag>
  );
}

export function Overlay({
  open,
  onClose,
  label,
  children
}: {
  open: boolean;
  onClose: () => void;
  label: string;
  children: ReactNode;
}) {
  const { reduce, fast, enter, ease } = useMotionTiming();
  return createPortal(
    <AnimatePresence>
      {open ? (
        <motion.div
          className="veil"
          role="dialog"
          aria-modal="true"
          aria-label={label}
          onClick={onClose}
          initial={reduce ? false : { opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={reduce ? undefined : { opacity: 0 }}
          transition={{ duration: reduce ? 0 : fast, ease }}
        >
          <motion.div
            className="veil-panel"
            onClick={(event) => event.stopPropagation()}
            initial={reduce ? false : { opacity: 0, scale: MOTION.scale.enter }}
            animate={{ opacity: 1, scale: 1 }}
            exit={reduce ? undefined : { opacity: 0, scale: MOTION.scale.enter }}
            transition={{ duration: reduce ? 0 : enter, ease }}
          >
            {children}
          </motion.div>
        </motion.div>
      ) : null}
    </AnimatePresence>,
    document.body
  );
}

export function DrawerFrame({
  open,
  label,
  children
}: {
  open: boolean;
  label: string;
  children: ReactNode;
}) {
  const { reduce, enter, ease } = useMotionTiming();
  return (
    <AnimatePresence>
      {open ? (
        <motion.aside
          className="drawer"
          aria-label={label}
          initial={reduce ? false : { opacity: 0, x: 28 }}
          animate={{ opacity: 1, x: 0 }}
          exit={reduce ? undefined : { opacity: 0, x: 20 }}
          transition={{ duration: reduce ? 0 : enter, ease }}
        >
          {children}
        </motion.aside>
      ) : null}
    </AnimatePresence>
  );
}

export function StatusBanner({
  error,
  ok,
  okText = "Saved."
}: {
  error: string | null;
  ok?: boolean;
  okText?: string;
}) {
  const { reduce, fast, ease } = useMotionTiming();
  const props: HTMLMotionProps<"p"> = {
    initial: reduce ? false : { opacity: 0, y: MOTION.distance.xs },
    animate: { opacity: 1, y: 0 },
    exit: reduce ? undefined : { opacity: 0, y: -4 },
    transition: { duration: reduce ? 0 : fast, ease }
  };
  return (
    <AnimatePresence mode="wait">
      {error ? (
        <motion.p key="err" className="note-err" role="alert" {...props}>
          {error}
        </motion.p>
      ) : ok ? (
        <motion.p key="ok" className="note-ok" role="status" {...props}>
          {okText}
        </motion.p>
      ) : null}
    </AnimatePresence>
  );
}

export function Skeleton({
  lines = 3,
  cards = 0,
  label
}: {
  lines?: number;
  cards?: number;
  label: string;
}) {
  return (
    <div className="sk-page" aria-busy="true" aria-live="polite">
      <span className="sr-only">{label}</span>
      <i className="sk sk-kicker" />
      <i className="sk sk-title" />
      {Array.from({ length: lines }, (_, index) => (
        <i key={index} className="sk sk-line" style={{ width: `${88 - index * 12}%` }} />
      ))}
      {cards > 0 ? (
        <div className="sk-grid">
          {Array.from({ length: cards }, (_, index) => (
            <i key={index} className="sk sk-card" />
          ))}
        </div>
      ) : null}
    </div>
  );
}
