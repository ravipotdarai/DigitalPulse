import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Executive — obsidian bioluminescence. Precision-instrument chrome over a void canvas. */
export const executiveTheme: ThemeTokens = {
  ...editorialTheme,
  id: "executive",
  label: "Executive",
  summary: "Obsidian void, cyan pulse, and instrument density.",
  fonts: {
    display: '"Syne", ui-sans-serif, sans-serif',
    sans: '"Outfit", ui-sans-serif, sans-serif',
    mono: '"JetBrains Mono", "IBM Plex Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      void: "#06080D",
      ink: "#F4F7FC",
      muted: "#8B93A7",
      panel: "#0E121C",
      raise: "#141A28",
      line: "rgba(255, 255, 255, 0.12)",
      lineStrong: "rgba(255, 255, 255, 0.2)",
      signal: "#00F2FE",
      signalSoft: "rgba(0, 242, 254, 0.16)",
      live: "#00F5A0",
      ok: "#00F5A0",
      warn: "#FFB800",
      crit: "#FF3B30",
      info: "#4FACFE",
      hold: "#7A8296"
    },
    light: {
      void: "#F3F5F8",
      ink: "#0B0E14",
      muted: "#4A5163",
      panel: "#ffffff",
      raise: "#ffffff",
      line: "rgba(11, 14, 20, 0.08)",
      lineStrong: "rgba(11, 14, 20, 0.16)",
      signal: "#0077A3",
      signalSoft: "rgba(0, 119, 163, 0.1)",
      live: "#0B8F62",
      ok: "#0B8F62",
      warn: "#9A6B00",
      crit: "#C62828",
      info: "#6B00B8",
      hold: "#6B7280"
    }
  },
  type: {
    ...editorialTheme.type,
    displaySize: "clamp(2.6rem, 6vw, 5rem)",
    displayTracking: "-0.02em",
    displayLeading: "0.94",
    kickerTracking: "0.22em"
  },
  space: { ...editorialTheme.space, section: "3.2rem", gutter: "1.5rem" },
  radius: { 1: "10px", 2: "16px" },
  elevation: {
    shadow: "0 24px 80px rgba(5, 6, 10, 0.12)",
    shadowDark: "0 30px 90px rgba(0, 0, 0, 0.55)"
  },
  motion: {
    ease: "cubic-bezier(0.16, 1, 0.3, 1)",
    duration: "280ms",
    durationSlow: "700ms",
    parallax: "8"
  },
  layout: { ...editorialTheme.layout, density: "0.96", grainOpacity: "0.055", heroMinHeight: "auto", navWidth: "16rem" }
};
