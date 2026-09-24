import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Future AI — near-black with an electric lime signal. Fallback layout until its own pass. */
export const futureAiTheme: ThemeTokens = {
  ...editorialTheme,
  id: "future-ai",
  label: "Future AI",
  summary: "Near-black with an electric lime signal.",
  fonts: {
    display: '"Syne", ui-sans-serif, sans-serif',
    sans: '"Outfit", ui-sans-serif, sans-serif',
    mono: '"DM Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      void: "#05060a",
      ink: "#f3f5ff",
      muted: "#a3abc4",
      panel: "#0b0e18",
      raise: "#121729",
      line: "rgba(200, 210, 255, 0.10)",
      lineStrong: "rgba(200, 210, 255, 0.18)",
      signal: "#c4f25c",
      signalSoft: "rgba(196, 242, 92, 0.13)",
      live: "#3ee0c5",
      ok: "#3ee0c5",
      warn: "#ffc857",
      crit: "#ff5d73",
      info: "#6fb1ff",
      hold: "#737c97"
    },
    light: {
      void: "#f5f7fb",
      ink: "#0a0d18",
      muted: "#525b72",
      panel: "#ffffff",
      raise: "#ffffff",
      line: "rgba(10, 13, 24, 0.08)",
      lineStrong: "rgba(10, 13, 24, 0.15)",
      signal: "#4a7a00",
      signalSoft: "rgba(74, 122, 0, 0.1)",
      live: "#0b8a76",
      ok: "#0b8a76",
      warn: "#a86b00",
      crit: "#cc2a45",
      info: "#2458c7",
      hold: "#747c92"
    }
  },
  type: {
    ...editorialTheme.type,
    displaySize: "clamp(3.1rem, 8vw, 6.4rem)",
    displayTracking: "-0.035em"
  },
  radius: { 1: "6px", 2: "12px" },
  layout: { ...editorialTheme.layout, grainOpacity: "0.04", density: "1" }
};
