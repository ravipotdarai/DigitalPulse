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

export function fluentFromTokens(tokens: ThemeTokens, scheme: ColorScheme): Theme {
  const colors = tokens.colors[scheme];
  const brand = hexSteps(colors.signal);
  const base = scheme === "dark" ? createDarkTheme(brand) : createLightTheme(brand);
  return {
    ...base,
    colorNeutralBackground1: colors.panel,
    colorNeutralBackground2: colors.raise,
    colorNeutralBackground3: colors.void,
    colorNeutralForeground1: colors.ink,
    colorNeutralForeground2: colors.muted,
    colorNeutralStroke1: colors.line,
    colorNeutralStroke2: colors.lineStrong,
    colorBrandForeground1: colors.signal,
    colorBrandBackground: colors.signal,
    colorBrandBackgroundHover: colors.signal,
    colorStrokeFocus2: colors.signal,
    fontFamilyBase: tokens.fonts.sans,
    fontFamilyNumeric: tokens.fonts.mono,
    borderRadiusSmall: tokens.radius[1],
    borderRadiusMedium: tokens.radius[2]
  };
}
