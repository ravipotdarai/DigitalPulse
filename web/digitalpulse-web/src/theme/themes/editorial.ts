import type { ThemeTokens } from "../types";

/** DigitalPulse Editorial — magazine composition, not a clone of any site. */
export const editorialTheme: ThemeTokens = {
  id: "editorial",
  label: "Editorial",
  summary: "Large type, paper surfaces, and cinematic sections.",
  fonts: {
    display: '"Fraunces", "Times New Roman", serif',
    sans: '"Source Sans 3", ui-sans-serif, sans-serif',
    mono: '"IBM Plex Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      void: "#060911",
      ink: "#eef2f8",
      muted: "#9ba7bd",
      panel: "#0c1221",
      raise: "#121a2e",
      line: "rgba(160, 185, 235, 0.10)",
      lineStrong: "rgba(160, 185, 235, 0.18)",
      signal: "#3ddbd9",
      signalSoft: "rgba(61, 219, 217, 0.13)",
      live: "#4ade9a",
      ok: "#4ade9a",
      warn: "#ffb547",
      crit: "#ff5f6d",
      info: "#7aa2ff",
      hold: "#6b7890"
    },
    light: {
      void: "#f4f6fa",
      ink: "#0b1220",
      muted: "#566179",
      panel: "#ffffff",
      raise: "#ffffff",
      line: "rgba(11, 18, 32, 0.08)",
      lineStrong: "rgba(11, 18, 32, 0.15)",
      signal: "#0b8f8d",
      signalSoft: "rgba(11, 143, 141, 0.1)",
      live: "#12925a",
      ok: "#12925a",
      warn: "#b86e00",
      crit: "#d42f45",
      info: "#2f5fd0",
      hold: "#7a8499"
    }
  },
  type: {
    bodySize: "16px",
    displaySize: "clamp(3.4rem, 9vw, 7.2rem)",
    displayTracking: "-0.045em",
    displayLeading: "0.9",
    kickerSize: "0.76rem",
    kickerTracking: "0.22em",
    pageTitleSize: "clamp(2rem, 5vw, 3.6rem)"
  },
  space: {
    1: "4px",
    2: "8px",
    3: "12px",
    4: "18px",
    5: "28px",
    6: "40px",
    7: "64px",
    section: "clamp(3.5rem, 8vw, 7rem)",
    gutter: "clamp(1.2rem, 4vw, 3.2rem)"
  },
  radius: { 1: "2px", 2: "4px" },
  elevation: {
    shadow: "0 28px 80px rgba(28, 23, 18, 0.08)",
    shadowDark: "0 30px 90px rgba(0, 0, 0, 0.5)"
  },
  motion: {
    ease: "cubic-bezier(0.16, 1, 0.3, 1)",
    duration: "280ms",
    durationSlow: "700ms",
    parallax: "18"
  },
  layout: {
    navWidth: "15rem",
    density: "1.15",
    heroMinHeight: "calc(100dvh - 4.5rem)",
    grainOpacity: "0.05"
  }
};
