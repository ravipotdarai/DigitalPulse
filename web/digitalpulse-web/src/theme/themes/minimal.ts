import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Fallback — spare surfaces. Independent Minimal visuals come later. */
export const minimalTheme: ThemeTokens = {
  ...editorialTheme,
  id: "minimal",
  label: "Minimal",
  summary: "Near-neutral fallback with no grain and square corners.",
  fonts: {
    display: '"IBM Plex Sans", ui-sans-serif, sans-serif',
    sans: '"IBM Plex Sans", ui-sans-serif, sans-serif',
    mono: '"IBM Plex Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      void: "#0a0a0a",
      ink: "#f2f2f2",
      muted: "#9a9a9a",
      panel: "#111111",
      raise: "#181818",
      line: "rgba(242, 242, 242, 0.1)",
      lineStrong: "rgba(242, 242, 242, 0.16)",
      signal: "#f2f2f2",
      signalSoft: "rgba(242, 242, 242, 0.08)",
      ok: "#9ad4b0",
      warn: "#d8c48a",
      crit: "#e0a0a0",
      info: "#a8c0d8",
      hold: "#888888"
    },
    light: {
      void: "#f7f7f5",
      ink: "#111111",
      muted: "#666666",
      panel: "#ffffff",
      raise: "#ffffff",
      line: "rgba(17, 17, 17, 0.1)",
      lineStrong: "rgba(17, 17, 17, 0.16)",
      signal: "#111111",
      signalSoft: "rgba(17, 17, 17, 0.06)",
      ok: "#1f6b45",
      warn: "#7a5b10",
      crit: "#9a2f2f",
      info: "#2c4d6e",
      hold: "#6a6a6a"
    }
  },
  type: {
    ...editorialTheme.type,
    displaySize: "clamp(2.2rem, 5vw, 3.8rem)",
    displayTracking: "-0.03em",
    displayLeading: "1"
  },
  radius: { 1: "0px", 2: "0px" },
  space: { ...editorialTheme.space, section: "2.5rem", gutter: "1.1rem" },
  layout: { ...editorialTheme.layout, grainOpacity: "0", density: "0.9", heroMinHeight: "auto", navWidth: "13.5rem" },
  motion: { ...editorialTheme.motion, duration: "160ms", parallax: "0" }
};
