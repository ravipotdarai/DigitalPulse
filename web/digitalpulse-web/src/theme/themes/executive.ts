import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Executive — boardroom graphite and gold, tighter density. Fallback layout until its own pass. */
export const executiveTheme: ThemeTokens = {
  ...editorialTheme,
  id: "executive",
  label: "Executive",
  summary: "Graphite and gold, tighter density.",
  fonts: {
    display: '"Libre Franklin", ui-sans-serif, sans-serif',
    sans: '"Libre Franklin", ui-sans-serif, sans-serif',
    mono: '"IBM Plex Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      void: "#0b0d12",
      ink: "#f4f5f8",
      muted: "#a9afbd",
      panel: "#12151d",
      raise: "#191d28",
      line: "rgba(220, 226, 240, 0.10)",
      lineStrong: "rgba(220, 226, 240, 0.18)",
      signal: "#e8bd5f",
      signalSoft: "rgba(232, 189, 95, 0.14)",
      live: "#5ad6a0",
      ok: "#5ad6a0",
      warn: "#f4a340",
      crit: "#ff6b6b",
      info: "#8fb4ff",
      hold: "#7c8394"
    },
    light: {
      void: "#f6f5f1",
      ink: "#12151b",
      muted: "#525866",
      panel: "#ffffff",
      raise: "#ffffff",
      line: "rgba(18, 21, 27, 0.08)",
      lineStrong: "rgba(18, 21, 27, 0.15)",
      signal: "#8f6310",
      signalSoft: "rgba(143, 99, 16, 0.1)",
      live: "#137a52",
      ok: "#137a52",
      warn: "#a65f00",
      crit: "#c62f3c",
      info: "#2c5bc4",
      hold: "#747b89"
    }
  },
  type: {
    ...editorialTheme.type,
    displaySize: "clamp(2.4rem, 6vw, 4.6rem)",
    displayTracking: "-0.03em",
    displayLeading: "1"
  },
  space: { ...editorialTheme.space, section: "3rem", gutter: "1.4rem" },
  layout: { ...editorialTheme.layout, density: "0.95", grainOpacity: "0.03", heroMinHeight: "auto" },
  motion: { ...editorialTheme.motion, parallax: "6" }
};
