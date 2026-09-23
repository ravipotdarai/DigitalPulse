import { Button } from "@fluentui/react-components";
import { motion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { PresenceLoop } from "../design/PresenceLoop";

export function LandingPage() {
  const navigate = useNavigate();
  return (
    <main>
      <section className="hero">
        <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}>
          <p className="hero-kicker">AI digital presence OS</p>
          <h1 className="display">
            AI that continuously manages your <em>digital presence.</em>
          </h1>
          <p className="hero-lead">
            Connect authorized platforms. Keep one identity. Detect drift. Approve what matters. Execute only what the provider allows. Verify the result. Then watch it.
          </p>
          <div className="hero-actions">
            <Button appearance="primary" size="large" onClick={() => navigate("/register")}>Create workspace</Button>
            <Button appearance="secondary" size="large" onClick={() => navigate("/login")}>I have access</Button>
          </div>
        </motion.div>
        <motion.div
          className="os-canvas"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.08, ease: [0.22, 1, 0.36, 1] }}
        >
          <span className="preview-chip">Operating loop</span>
          <PresenceLoop active="identity" />
        </motion.div>
      </section>
      <section style={{ position: "relative", zIndex: 1, padding: "0 1.15rem 4rem" }}>
        <p className="hero-kicker">The loop</p>
        <h2 className="display" style={{ fontSize: "clamp(2rem, 4vw, 3.2rem)", maxWidth: "16ch" }}>One identity. Authorized action only.</h2>
        <p className="hero-lead">
          DigitalPulse does not invent live connections. Every later phase hangs on the identity you keep here.
        </p>
        <div className="band band-3">
          {["Identity first", "Authorization first", "Evidence before claims"].map((title) => (
            <article className="panel" key={title}>
              <h3>{title}</h3>
              <p style={{ color: "var(--muted)", margin: 0 }}>
                {title === "Identity first"
                  ? "The business record is the source of truth for every listing comparison."
                  : title === "Authorization first"
                    ? "Adapters expose only what the provider allows. Unsupported actions stay out."
                    : "Restricted facts never publish. Low-confidence output waits for approval."}
              </p>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
