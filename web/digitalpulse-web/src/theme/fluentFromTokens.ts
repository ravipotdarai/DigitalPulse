import { createLightTheme, createDarkTheme, type BrandVariants, type Theme } from "@fluentui/react-components";
import type { ColorScheme, ThemeTokens } from "./types";

function hexRgb(hex: string): [number, number, number] | null {
  const value = hex.replace("#", "").trim();
  if (!/^[0-9a-f]{6}$/i.test(value)) return null;
  return [Number.parseInt(value.slice(0, 2), 16), Number.parseInt(value.slice(2, 4), 16), Number.parseInt(value.slice(4, 6), 16)];
}

function rgbHex(r: number, g: number, b: number) {
  return `#${[r, g, b].map((part) => Math.max(0, Math.min(255, Math.round(part))).toString(16).padStart(2, "0")).join("")}`;
}

function mixHex(from: string, to: string, amount: number, fallback: string) {
  const a = hexRgb(from);
  const b = hexRgb(to);
  if (!a || !b) return fallback;
  return rgbHex(a[0] + (b[0] - a[0]) * amount, a[1] + (b[1] - a[1]) * amount, a[2] + (b[2] - a[2]) * amount);
}

function brandRamp(signal: string, voidColor: string, ink: string): BrandVariants {
  const brand = {} as BrandVariants;
  const rise = [10, 20, 30, 40, 50, 60, 70, 80] as const;
  const fade = [90, 100, 110, 120, 130, 140, 150, 160] as const;
  rise.forEach((step, index) => {
    brand[step] = mixHex(voidColor, signal, (index + 1) / rise.length, signal);
  });
  brand[80] = signal;
  fade.forEach((step, index) => {
    brand[step] = mixHex(signal, ink, (index + 1) / (fade.length + 2), signal);
  });
  return brand;
}

function mix(color: string, amount: number, base: string) {
  return `color-mix(in srgb, ${color} ${amount}%, ${base})`;
}

/** Fluent keeps accessibility and behaviour; every visual decision comes from DigitalPulse tokens. */
export function fluentFromTokens(tokens: ThemeTokens, scheme: ColorScheme): Theme {
  const c = tokens.colors[scheme];
  const field = mix(c.ink, scheme === "dark" ? 8 : 4, c.raise);
  const stroke = mix(c.ink, scheme === "dark" ? 22 : 16, "transparent");
  const base = scheme === "dark" ? createDarkTheme(brandRamp(c.signal, c.void, c.ink)) : createLightTheme(brandRamp(c.signal, c.ink, c.void));
  return {
    ...base,
    colorNeutralBackground1: field,
    colorNeutralBackground1Hover: mix(c.ink, scheme === "dark" ? 12 : 7, c.raise),
    colorNeutralBackground1Pressed: mix(c.ink, scheme === "dark" ? 16 : 10, c.raise),
    colorNeutralBackground1Selected: mix(c.signal, 12, c.raise),
    colorNeutralBackground2: c.raise,
    colorNeutralBackground3: field,
    colorNeutralBackground3Hover: mix(c.ink, 12, c.raise),
    colorNeutralBackgroundDisabled: mix(c.ink, 6, c.panel),
    colorNeutralForeground1: c.ink,
    colorNeutralForeground1Hover: c.ink,
    colorNeutralForeground2: mix(c.ink, 78, c.void),
    colorNeutralForeground2Hover: c.ink,
    colorNeutralForeground2BrandHover: c.signal,
    colorNeutralForeground3: c.muted,
    colorNeutralForeground4: c.muted,
    colorNeutralForegroundDisabled: mix(c.ink, 42, c.void),
    colorNeutralForegroundOnBrand: scheme === "dark" ? c.void : c.ink,
    colorNeutralStroke1: stroke,
    colorNeutralStroke1Hover: mix(c.ink, 34, "transparent"),
    colorNeutralStroke2: c.line,
    colorNeutralStrokeAccessible: mix(c.ink, 28, "transparent"),
    colorNeutralStrokeAccessibleHover: c.signal,
    colorNeutralStrokeDisabled: mix(c.ink, 12, "transparent"),
    colorCompoundBrandStroke: c.signal,
    colorCompoundBrandStrokeHover: c.signal,
    colorBrandForeground1: c.signal,
    colorBrandForeground2: c.signal,
    colorBrandBackground: c.signal,
    colorBrandBackgroundHover: mix(c.signal, 88, c.ink),
    colorBrandBackgroundPressed: mix(c.signal, 78, c.void),
    colorBrandBackgroundSelected: c.signal,
    colorSubtleBackgroundHover: c.signalSoft,
    colorSubtleBackgroundPressed: mix(c.signal, 20, "transparent"),
    colorTransparentBackgroundHover: c.signalSoft,
    colorStrokeFocus1: c.void,
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
