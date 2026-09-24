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
      void: "#0b0907",
      ink: "#f3ecdf",
      muted: "#b4a894",
      panel: "#14110d",
      raise: "#1c1813",
      line: "rgba(243, 236, 223, 0.1)",
      lineStrong: "rgba(243, 236, 223, 0.16)",
      signal: "#d4b483",
      signalSoft: "rgba(212, 180, 131, 0.14)",
      ok: "#8fbf9a",
      warn: "#e0c07a",
      crit: "#e08a7a",
      info: "#9bb4c9",
      hold: "#8d8578"
    },
    light: {
      void: "#f4efe6",
      ink: "#1c1712",
      muted: "#6b6256",
      panel: "#fffaf2",
      raise: "#ffffff",
      line: "rgba(28, 23, 18, 0.1)",
      lineStrong: "rgba(28, 23, 18, 0.16)",
      signal: "#8b5a2b",
      signalSoft: "rgba(139, 90, 43, 0.1)",
      ok: "#2f6b4a",
      warn: "#8a6a16",
      crit: "#a33b32",
      info: "#3b5c7a",
      hold: "#6f675c"
    }
  },
  type: {
    bodySize: "16px",
    displaySize: "clamp(3.4rem, 9vw, 7.2rem)",
    displayTracking: "-0.045em",
    displayLeading: "0.9",
    kickerSize: "0.7rem",
    kickerTracking: "0.28em",
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
    grainOpacity: "0.1"
  }
};
