import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Fallback tokens — ready for an independent Executive visual later. */
export const executiveTheme: ThemeTokens = {
  ...editorialTheme,
  id: "executive",
  label: "Executive",
  summary: "Cooler surfaces and tighter density. Visuals land in a later pass.",
  fonts: {
    display: '"Libre Franklin", ui-sans-serif, sans-serif',
    sans: '"Libre Franklin", ui-sans-serif, sans-serif',
    mono: '"IBM Plex Mono", ui-monospace, monospace'
  },
  colors: {
    dark: {
      ...editorialTheme.colors.dark,
      void: "#0b1218",
      panel: "#121a22",
      raise: "#18222c",
      signal: "#c4a36a",
      signalSoft: "rgba(196, 163, 106, 0.14)"
    },
    light: {
      ...editorialTheme.colors.light,
      void: "#eef2f5",
      panel: "#ffffff",
      signal: "#1f3d5c"
    }
  },
  type: {
    ...editorialTheme.type,
    displaySize: "clamp(2.4rem, 6vw, 4.6rem)",
    displayTracking: "-0.03em"
  },
  space: { ...editorialTheme.space, section: "3rem", gutter: "1.4rem" },
  layout: { ...editorialTheme.layout, density: "0.95", grainOpacity: "0.04", heroMinHeight: "auto" },
  motion: { ...editorialTheme.motion, parallax: "6" }
};
