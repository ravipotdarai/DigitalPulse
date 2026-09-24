import { Button } from "@fluentui/react-components";
import { motion, useReducedMotion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { PresenceLoop } from "../design/PresenceLoop";
import { useParallax, useTheme } from "../theme";

const CHAPTERS = [
  {
    num: "02",
    kicker: "The record",
    title: "One identity. Authorized action only.",
    lead: "DigitalPulse does not invent live connections. Every later phase hangs on the identity you keep here.",
    stories: [
      { title: "Identity first", body: "The business record is the source of truth for every listing comparison." },
      { title: "Authorization first", body: "Adapters expose only what the provider allows. Unsupported actions stay out." },
      { title: "Evidence before claims", body: "Restricted facts never publish. Low-confidence output waits for approval." }
    ]
  }
];

export function LandingPage() {
  const navigate = useNavigate();
  const { tokens } = useTheme();
  const reduce = useReducedMotion();
  const offset = useParallax(Number(tokens.motion.parallax));
  const duration = reduce ? 0 : Number.parseFloat(tokens.motion.durationSlow) / 1000;

  return (
    <main className="landing">
      <section className="hero">
        <motion.div
          className="hero-copy"
          initial={reduce ? false : { opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration }}
        >
          <p className="chapter-num">01</p>
          <p className="hero-kicker">Presence OS</p>
          <h1 className="display">
            Continuously managed <em>digital presence.</em>
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
          className="os-canvas parallax-field will-parallax"
          style={{ transform: reduce ? undefined : `translate3d(0, ${offset}px, 0)` }}
          initial={reduce ? false : { opacity: 0, y: 28 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration, delay: reduce ? 0 : 0.08 }}
        >
          <span className="preview-chip">Operating loop</span>
          <PresenceLoop active="identity" />
        </motion.div>
      </section>

      {CHAPTERS.map((chapter) => (
        <section className="chapter" key={chapter.num}>
          <p className="chapter-num">{chapter.num}</p>
          <p className="hero-kicker">{chapter.kicker}</p>
          <h2 className="display display-section">{chapter.title}</h2>
          <p className="hero-lead">{chapter.lead}</p>
          <div className="band band-3">
            {chapter.stories.map((story) => (
              <article className="panel" key={story.title}>
                <h3>{story.title}</h3>
                <p className="ink-muted flush">{story.body}</p>
              </article>
            ))}
          </div>
        </section>
      ))}
    </main>
  );
}
