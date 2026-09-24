import { THEME_IDS, type ColorScheme, type ThemeId, type ThemePreference } from "./types";

const DEVICE_KEY = "dp.theme";
const userKey = (userId: string) => `dp.theme.user.${userId}`;

export const defaultPreference: ThemePreference = { id: "editorial", scheme: "dark" };

function parse(raw: string | null): ThemePreference | null {
  if (!raw) return null;
  try {
    const value = JSON.parse(raw) as Partial<ThemePreference>;
    if (value.id && THEME_IDS.includes(value.id as ThemeId) && (value.scheme === "dark" || value.scheme === "light")) {
      return { id: value.id as ThemeId, scheme: value.scheme };
    }
  } catch {
    /* ignore corrupt preference */
  }
  return null;
}

export function readThemePreference(userId?: string | null): ThemePreference {
  if (userId) {
    const owned = parse(localStorage.getItem(userKey(userId)));
    if (owned) return owned;
  }
  return parse(localStorage.getItem(DEVICE_KEY)) ?? defaultPreference;
}

export function writeThemePreference(preference: ThemePreference, userId?: string | null) {
  const payload = JSON.stringify(preference);
  localStorage.setItem(DEVICE_KEY, payload);
  if (userId) {
    localStorage.setItem(userKey(userId), payload);
  }
}

export function isThemeId(value: string): value is ThemeId {
  return THEME_IDS.includes(value as ThemeId);
}

export function isColorScheme(value: string): value is ColorScheme {
  return value === "dark" || value === "light";
}
