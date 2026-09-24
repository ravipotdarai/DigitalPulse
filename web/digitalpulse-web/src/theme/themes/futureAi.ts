import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Fallback — current Pulse OS orange on void, for later Future AI work. */
export const futureAiTheme: ThemeTokens = {
  ...editorialTheme,
  id: "future-ai",
  label: "Future AI",
  summary: "Signal-led fallback. A dedicated Future AI language comes later.",
  fonts: {
    display: '"Syne", ui-sans-serif, sans-serif',
    sans: '"Outfit", ui-sans-serif, sans-serif',
    mono: '"DM Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      void: "#07080b",
      ink: "#ece7de",
      muted: "#9aa093",
      panel: "#10131a",
      raise: "#171b24",
      line: "rgba(236, 231, 222, 0.08)",
      lineStrong: "rgba(236, 231, 222, 0.14)",
      signal: "#e46a2e",
      signalSoft: "rgba(228, 106, 46, 0.16)",
      ok: "#4ecf97",
      warn: "#e8c15a",
      crit: "#f07171",
      info: "#7aa7e8",
      hold: "#8b8478"
    },
    light: {
      void: "#f3efe6",
      ink: "#0e1118",
      muted: "#5c6158",
      panel: "#fffdf8",
      raise: "#ffffff",
      line: "rgba(14, 17, 24, 0.1)",
      lineStrong: "rgba(14, 17, 24, 0.16)",
      signal: "#c45c26",
      signalSoft: "rgba(196, 92, 38, 0.12)",
      ok: "#247a56",
      warn: "#9a6d14",
      crit: "#b4232c",
      info: "#2c5d9e",
      hold: "#6a6256"
    }
  },
  type: {
    ...editorialTheme.type,
    displaySize: "clamp(3.1rem, 8vw, 6.4rem)"
  },
  radius: { 1: "4px", 2: "8px" },
  layout: { ...editorialTheme.layout, grainOpacity: "0.12", density: "1" }
};
