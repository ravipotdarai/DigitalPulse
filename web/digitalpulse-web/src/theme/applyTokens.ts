import type { ColorScheme, ThemeTokens } from "./types";

const cssVarMap: Record<string, string> = {
  void: "--void",
  ink: "--ink",
  muted: "--muted",
  panel: "--panel",
  raise: "--raise",
  line: "--line",
  lineStrong: "--line-strong",
  signal: "--signal",
  signalSoft: "--signal-soft",
  live: "--live",
  ok: "--ok",
  warn: "--warn",
  crit: "--crit",
  info: "--info",
  hold: "--hold"
};

export function tokenCssVars(tokens: ThemeTokens, scheme: ColorScheme): Record<string, string> {
  const colors = tokens.colors[scheme];
  const vars: Record<string, string> = {
    "--font-display": tokens.fonts.display,
    "--font-sans": tokens.fonts.sans,
    "--font-mono": tokens.fonts.mono,
    "--space-1": tokens.space[1],
    "--space-2": tokens.space[2],
    "--space-3": tokens.space[3],
    "--space-4": tokens.space[4],
    "--space-5": tokens.space[5],
    "--space-6": tokens.space[6],
    "--space-7": tokens.space[7],
    "--section-y": tokens.space.section,
    "--gutter": tokens.space.gutter,
    "--r1": tokens.radius[1],
    "--r2": tokens.radius[2],
    "--r-1": tokens.radius[1],
    "--r-2": tokens.radius[2],
    "--fontFamilyBase": tokens.fonts.sans,
    "--shadow": tokens.elevation.shadow,
    "--shadow-dark": tokens.elevation.shadowDark,
    "--ease": tokens.motion.ease,
    "--dur": tokens.motion.duration,
    "--dur-slow": tokens.motion.durationSlow,
    "--parallax": tokens.motion.parallax,
    "--nav-col": tokens.layout.navWidth,
    "--density": tokens.layout.density,
    "--hero-min": tokens.layout.heroMinHeight,
    "--grain-opacity": tokens.layout.grainOpacity,
    "--display-size": tokens.type.displaySize,
    "--display-tracking": tokens.type.displayTracking,
    "--display-leading": tokens.type.displayLeading,
    "--kicker-size": tokens.type.kickerSize,
    "--kicker-tracking": tokens.type.kickerTracking,
    "--body-size": tokens.type.bodySize,
    "--page-title-size": tokens.type.pageTitleSize
  };

  for (const [key, cssName] of Object.entries(cssVarMap)) {
    vars[cssName] = colors[key as keyof typeof colors];
  }

  return vars;
}

export function applyTokens(root: HTMLElement, tokens: ThemeTokens, scheme: ColorScheme) {
  const vars = tokenCssVars(tokens, scheme);
  root.dataset.theme = tokens.id;
  root.dataset.scheme = scheme;
  root.classList.toggle("dark", scheme === "dark");
  for (const [name, value] of Object.entries(vars)) {
    root.style.setProperty(name, value);
  }
}
