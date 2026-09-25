import { Button } from "../design/Button";
import { motion } from "framer-motion";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CORE_LOOP, LoopTrack, type LoopStage, type StageState } from "../design/LoopTrack";
import { Reveal, useMotionTiming } from "../design/motion";
import { PlatformEcosystem, type EcosystemNode } from "../design/PlatformEcosystem";
import type { LinkState } from "../design/platforms";
import { useParallax, useTheme } from "../theme";

const ILLUSTRATION: EcosystemNode[] = [
  { code: "WEBSITE", name: "Website", category: "Owned channel", state: "live", detail: "The official site anchors every comparison: name, phone, services, schema." },
  { code: "GOOGLE", name: "Google", category: "Search & Maps", state: "live", detail: "Business Profile hours, categories and posts checked against your record." },
  { code: "INSTAGRAM", name: "Instagram", category: "Social", state: "syncing", detail: "Captions, carousels and reel scripts drafted from approved projects." },
  { code: "YOUTUBE", name: "YouTube", category: "Video", state: "idle", detail: "Channel metadata kept consistent with the brand voice." },
  { code: "WHATSAPP", name: "WhatsApp", category: "Messaging", state: "idle", detail: "Template drafts only, sent through the official Cloud API once approved." },
  { code: "JUSTDIAL", name: "Directories", category: "IndiaMART · Justdial", state: "warning", detail: "Assisted playbooks when a directory offers no official write." },
  { code: "LINKEDIN", name: "LinkedIn", category: "Professional", state: "live", detail: "Case studies from verified projects, within their permission scope." },
  { code: "FACEBOOK", name: "Facebook", category: "Social", state: "idle", detail: "Page posts drafted, approved, and held until the provider allows publishing." }
];

const NEXT_STATE: Record<LinkState, LinkState> = { idle: "syncing", syncing: "live", live: "idle", warning: "syncing", failing: "syncing" };

const STORY: { stages: [LoopStage, LoopStage]; title: string; body: string }[] = [
  { stages: ["Connect", "Understand"], title: "One identity, every platform", body: "Authorize each platform through its official adapter. DigitalPulse keeps one canonical record of who the business is." },
  { stages: ["Detect", "Create"], title: "Signals with evidence", body: "Every check compares the record to what customers actually see. Drafts are assembled from facts you approved, never invented." },
  { stages: ["Approve", "Execute"], title: "Nothing moves without you", body: "Approval respects permission scope. Execution happens only where the provider genuinely allows it." },
  { stages: ["Verify", "Monitor"], title: "Proof, then vigilance", body: "Changes are verified against the source, then watched so drift is caught before customers notice." }
];

export function LandingPage() {
  const navigate = useNavigate();
  const { tokens } = useTheme();
  const { reduce, slow, ease } = useMotionTiming();
  const offset = useParallax(Number(tokens.motion.parallax));
  const [step, setStep] = useState(0);
  const [demo, setDemo] = useState<EcosystemNode[]>(ILLUSTRATION);

  function toggle(code: string) {
    setDemo((nodes) => nodes.map((node) => (node.code === code ? { ...node, state: NEXT_STATE[node.state] } : node)));
    const current = demo.find((node) => node.code === code);
    if (current && NEXT_STATE[current.state] === "syncing") {
      window.setTimeout(() => {
        setDemo((nodes) => nodes.map((node) => (node.code === code && node.state === "syncing" ? { ...node, state: "live" } : node)));
      }, 1600);
    }
  }

  useEffect(() => {
    if (reduce) return;
    const timer = window.setInterval(() => setStep((value) => (value + 1) % CORE_LOOP.length), 1800);
    return () => window.clearInterval(timer);
  }, [reduce]);

  const states = Object.fromEntries(
    CORE_LOOP.map((stage, index) => [stage, index < step ? "done" : index === step ? "now" : "next"])
  ) as Record<LoopStage, StageState>;

  const rise = {
    hidden: reduce ? { opacity: 1, y: 0 } : { opacity: 0, y: 28 },
    shown: { opacity: 1, y: 0, transition: { duration: slow, ease } }
  };

  return (
    <main className="landing">
      <section className="hero">
        <motion.div
          className="hero-copy"
          initial="hidden"
          animate="shown"
          variants={{
            hidden: {},
            shown: { transition: { staggerChildren: reduce ? 0 : 0.12, delayChildren: reduce ? 0 : 0.06 } }
          }}
        >
          <motion.p className="hero-kicker" variants={rise}>The digital presence operating system</motion.p>
          <motion.h1 className="display" variants={rise}>
            Your business, <em>present everywhere.</em> Correct everywhere.
          </motion.h1>
          <motion.p className="hero-lead" variants={rise}>
            DigitalPulse connects the platforms a business lives on, detects where its presence has drifted, and fixes only what you approve and the provider allows.
          </motion.p>
          <motion.div className="hero-actions" variants={rise}>
            <Button appearance="primary" size="large" onClick={() => navigate("/register")}>Create workspace</Button>
            <Button appearance="secondary" size="large" onClick={() => navigate("/login")}>Sign in</Button>
          </motion.div>
        </motion.div>
        <motion.div
          className="hero-eco"
          initial={reduce ? false : { opacity: 0, scale: 0.94, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={{ duration: slow * 1.35, delay: reduce ? 0 : 0.18, ease }}
        >
          <div
            className="parallax-field will-parallax"
            style={{ transform: reduce ? undefined : `translate3d(0, ${offset}px, 0)` }}
          >
            <PlatformEcosystem
              compact
              centerLabel="Your business"
              centerMeta={`${demo.filter((node) => node.state === "live").length} linked`}
              nodes={demo}
              onSelect={toggle}
              hint="Click a mark to preview authorizing this platform"
              caption="Interactive illustration. Hover a platform, click to connect it. Nothing here is live data."
            />
          </div>
        </motion.div>
      </section>

      <section className="story-loop" aria-labelledby="story-title">
        <Reveal>
          <p className="hero-kicker">The core loop</p>
          <h2 id="story-title" className="display display-section">Eight steps. Repeated, for every platform.</h2>
        </Reveal>
        <Reveal delay={0.1}>
          <LoopTrack states={states} compact />
        </Reveal>
        <div className="story-grid">
          {STORY.map((item, index) => (
            <Reveal key={item.title} delay={index * 0.08} as="article">
              <div className="story-step">
                <span>{String(index * 2 + 1).padStart(2, "0")} {item.stages[0]} · {String(index * 2 + 2).padStart(2, "0")} {item.stages[1]}</span>
                <h3>{item.title}</h3>
                <p>{item.body}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </section>
    </main>
  );
}
