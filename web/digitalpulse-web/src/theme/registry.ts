import { customTheme } from "./themes/custom";
import { editorialTheme } from "./themes/editorial";
import { executiveTheme } from "./themes/executive";
import { futureAiTheme } from "./themes/futureAi";
import { minimalTheme } from "./themes/minimal";
import { THEME_IDS, type ThemeId, type ThemeTokens } from "./types";

const catalog: Record<ThemeId, ThemeTokens> = {
  editorial: editorialTheme,
  executive: executiveTheme,
  "future-ai": futureAiTheme,
  minimal: minimalTheme,
  custom: customTheme
};

export const themeCatalog = THEME_IDS.map((id) => catalog[id]);

export function resolveTheme(id: string | null | undefined): ThemeTokens {
  if (id && THEME_IDS.includes(id as ThemeId)) {
    return catalog[id as ThemeId];
  }
  return editorialTheme;
}
