export const THEME_IDS = ["editorial", "executive", "future-ai", "minimal", "custom"] as const;
export type ThemeId = (typeof THEME_IDS)[number];
export type ColorScheme = "dark" | "light";

export type ThemeTokens = {
  id: ThemeId;
  label: string;
  summary: string;
  fonts: { display: string; sans: string; mono: string };
  colors: {
    dark: ColorSet;
    light: ColorSet;
  };
  type: {
    bodySize: string;
    displaySize: string;
    displayTracking: string;
    displayLeading: string;
    kickerSize: string;
    kickerTracking: string;
    pageTitleSize: string;
  };
  space: {
    1: string;
    2: string;
    3: string;
    4: string;
    5: string;
    6: string;
    7: string;
    section: string;
    gutter: string;
  };
  radius: { 1: string; 2: string };
  elevation: { shadow: string; shadowDark: string };
  motion: {
    ease: string;
    duration: string;
    durationSlow: string;
    parallax: string;
  };
  layout: {
    navWidth: string;
    density: string;
    heroMinHeight: string;
    grainOpacity: string;
  };
};

export type ColorSet = {
  void: string;
  ink: string;
  muted: string;
  panel: string;
  raise: string;
  line: string;
  lineStrong: string;
  signal: string;
  signalSoft: string;
  live: string;
  ok: string;
  warn: string;
  crit: string;
  info: string;
  hold: string;
};

export type ThemePreference = {
  id: ThemeId;
  scheme: ColorScheme;
};
