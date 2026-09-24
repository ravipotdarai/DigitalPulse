import { createLightTheme, createDarkTheme, type BrandVariants, type Theme } from "@fluentui/react-components";
import type { ColorScheme, ThemeTokens } from "./types";

function hexSteps(hex: string): BrandVariants {
  const steps = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160] as const;
  const brand = {} as BrandVariants;
  for (const step of steps) {
    brand[step] = hex;
  }
  return brand;
}

function mix(color: string, amount: number, base: string) {
  return `color-mix(in srgb, ${color} ${amount}%, ${base})`;
}

/** Fluent keeps accessibility and behaviour; every visual decision comes from DigitalPulse tokens. */
export function fluentFromTokens(tokens: ThemeTokens, scheme: ColorScheme): Theme {
  const c = tokens.colors[scheme];
  const base = scheme === "dark" ? createDarkTheme(hexSteps(c.signal)) : createLightTheme(hexSteps(c.signal));
  return {
    ...base,
    colorNeutralBackground1: c.panel,
    colorNeutralBackground1Hover: mix(c.ink, 5, c.panel),
    colorNeutralBackground1Pressed: mix(c.ink, 9, c.panel),
    colorNeutralBackground2: c.raise,
    colorNeutralBackground3: c.void,
    colorNeutralForeground1: c.ink,
    colorNeutralForeground1Hover: c.ink,
    colorNeutralForeground2: mix(c.ink, 78, c.void),
    colorNeutralForeground2Hover: c.ink,
    colorNeutralForeground2BrandHover: c.signal,
    colorNeutralForeground3: c.muted,
    colorNeutralForegroundOnBrand: c.void,
    colorNeutralStroke1: c.lineStrong,
    colorNeutralStroke1Hover: mix(c.ink, 30, "transparent"),
    colorNeutralStroke2: c.line,
    colorNeutralStrokeAccessible: c.muted,
    colorNeutralStrokeAccessibleHover: c.signal,
    colorCompoundBrandStroke: c.signal,
    colorCompoundBrandStrokeHover: c.signal,
    colorBrandForeground1: c.signal,
    colorBrandForeground2: c.signal,
    colorBrandBackground: c.signal,
    colorBrandBackgroundHover: mix(c.signal, 88, c.ink),
    colorBrandBackgroundPressed: mix(c.signal, 78, c.void),
    colorSubtleBackgroundHover: c.signalSoft,
    colorSubtleBackgroundPressed: mix(c.signal, 20, "transparent"),
    colorTransparentBackgroundHover: c.signalSoft,
    colorStrokeFocus2: c.signal,
    fontFamilyBase: tokens.fonts.sans,
    fontFamilyNumeric: tokens.fonts.mono,
    fontFamilyMonospace: tokens.fonts.mono,
    borderRadiusSmall: tokens.radius[1],
    borderRadiusMedium: tokens.radius[2],
    borderRadiusLarge: tokens.radius[2],
    borderRadiusXLarge: tokens.radius[2]
  };
}
