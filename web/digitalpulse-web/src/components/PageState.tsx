import { Skeleton, useMotionTiming } from "../design/motion";
import { motion } from "framer-motion";

export function PageState({
  title,
  detail,
  mode = "info"
}: {
  title: string;
  detail?: string;
  mode?: "loading" | "empty" | "error" | "info";
}) {
  const { reduce, enter, ease } = useMotionTiming();

  if (mode === "loading") {
    return (
      <div className="state is-loading" role="status">
        <Skeleton label={title} lines={3} cards={4} />
      </div>
    );
  }

  return (
    <motion.div
      className={`state is-${mode}`}
      role={mode === "error" ? "alert" : "status"}
      initial={reduce ? false : { opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: enter, ease }}
    >
      <h2 className="display">{title}</h2>
      {detail ? <p>{detail}</p> : null}
    </motion.div>
  );
}
