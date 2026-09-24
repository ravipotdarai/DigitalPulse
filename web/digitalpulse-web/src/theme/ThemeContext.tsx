import { createContext, useContext } from "react";
import type { ColorScheme, ThemeId, ThemeTokens } from "./types";
import { editorialTheme } from "./themes/editorial";

export type ThemeContextValue = {
  id: ThemeId;
  scheme: ColorScheme;
  tokens: ThemeTokens;
  setId: (id: ThemeId) => void;
  setScheme: (scheme: ColorScheme) => void;
};

export const ThemeContext = createContext<ThemeContextValue>({
  id: "editorial",
  scheme: "dark",
  tokens: editorialTheme,
  setId: () => undefined,
  setScheme: () => undefined
});

export function useTheme() {
  return useContext(ThemeContext);
}
