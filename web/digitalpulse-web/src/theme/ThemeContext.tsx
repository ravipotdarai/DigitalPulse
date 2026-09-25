import { createContext, useContext } from "react";
import type { ColorScheme, ThemeId, ThemeTokens } from "./types";
import { executiveTheme } from "./themes/executive";

export type ThemeContextValue = {
  id: ThemeId;
  scheme: ColorScheme;
  tokens: ThemeTokens;
  setId: (id: ThemeId) => void;
  setScheme: (scheme: ColorScheme) => void;
};

export const ThemeContext = createContext<ThemeContextValue>({
  id: "executive",
  scheme: "dark",
  tokens: executiveTheme,
  setId: () => undefined,
  setScheme: () => undefined
});

export function useTheme() {
  return useContext(ThemeContext);
}
