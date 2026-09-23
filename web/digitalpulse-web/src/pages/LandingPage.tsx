import { Button } from "@fluentui/react-components";
import { motion } from "framer-motion";
import { useNavigate } from "react-router-dom";

export function LandingPage() {
  const navigate = useNavigate();
  return (
    <main className="relative overflow-hidden">
      <section className="relative mx-auto grid min-h-[88dvh] max-w-6xl items-center gap-10 px-5 py-10 md:grid-cols-2 md:px-8">
        <div className="pointer-events-none absolute left-1/2 top-24 hidden h-[420px] w-[420px] -translate-x-1/4 md:block">
          {[0, 1, 2].map((i) => (
            <div key={i} className="pulse-ring absolute inset-0" style={{ animationDelay: `${i * 1.4}s` }} />
          ))}
        </div>
        <motion.div initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.7 }}>
          <p className="mb-4 text-sm uppercase tracking-[0.28em]" style={{ color: "var(--signal)" }}>
            Presence operating system
          </p>
          <h1 className="display text-5xl leading-[0.95] md:text-7xl">
            Hear your brand
            <span className="block italic" style={{ color: "var(--signal)" }}>pulse online.</span>
          </h1>
          <p className="mt-6 max-w-md text-lg" style={{ color: "var(--muted)" }}>
            Connect authorized platforms, keep identity honest, and let DigitalPulse detect, approve, and verify every change — from phone to desktop.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Button appearance="primary" size="large" onClick={() => navigate("/register")}>Create your workspace</Button>
            <Button appearance="secondary" size="large" onClick={() => navigate("/login")}>I already have access</Button>
          </div>
        </motion.div>
        <motion.div
          initial={{ opacity: 0, x: 30 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.8, delay: 0.15 }}
          className="relative rounded-3xl border p-5 shadow-2xl"
          style={{ background: "var(--card)", borderColor: "var(--stroke)" }}
        >
          <div className="mb-4 flex items-center justify-between text-xs uppercase tracking-[0.2em]" style={{ color: "var(--muted)" }}>
            <span>Live observatory</span>
            <span className="rounded-full px-2 py-1" style={{ background: "color-mix(in srgb, var(--signal) 18%, transparent)", color: "var(--signal)" }}>preview</span>
          </div>
          <div className="space-y-3">
            {["NAP mismatch on website", "Google profile waiting approval", "Assisted IndiaMART checklist"].map((item, i) => (
              <motion.div
                key={item}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.35 + i * 0.12 }}
                className="flex items-center justify-between rounded-2xl border px-4 py-3"
                style={{ borderColor: "var(--stroke)" }}
              >
                <span>{item}</span>
                <span className="text-xs" style={{ color: "var(--signal)" }}>{i === 1 ? "Pending" : "Detected"}</span>
              </motion.div>
            ))}
          </div>
        </motion.div>
      </section>
    </main>
  );
}
