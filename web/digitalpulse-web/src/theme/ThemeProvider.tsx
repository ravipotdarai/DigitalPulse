import { FluentProvider } from "@fluentui/react-components";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { applyTokens } from "./applyTokens";
import { fluentFromTokens } from "./fluentFromTokens";
import { readThemePreference, writeThemePreference } from "./persist";
import { resolveTheme, themeCatalog } from "./registry";
import { ThemeContext } from "./ThemeContext";
import type { ColorScheme, ThemeId } from "./types";

type Props = {
  userId?: string | null;
  children: ReactNode;
};

export function ThemeProvider({ userId, children }: Props) {
  const [id, setIdState] = useState<ThemeId>(() => readThemePreference(userId).id);
  const [scheme, setSchemeState] = useState<ColorScheme>(() => readThemePreference(userId).scheme);
  const tokens = useMemo(() => resolveTheme(id), [id]);
  const fluent = useMemo(() => fluentFromTokens(tokens, scheme), [tokens, scheme]);

  useEffect(() => {
    const next = readThemePreference(userId);
    setIdState(next.id);
    setSchemeState(next.scheme);
  }, [userId]);

  useEffect(() => {
    applyTokens(document.documentElement, tokens, scheme);
    writeThemePreference({ id, scheme }, userId);
  }, [id, scheme, tokens, userId]);

  useEffect(() => {
    if (themeCatalog.length < 4) {
      throw new Error("Theme catalog is incomplete.");
    }
  }, []);

  const value = useMemo(
    () => ({
      id,
      scheme,
      tokens,
      setId: setIdState,
      setScheme: setSchemeState
    }),
    [id, scheme, tokens]
  );

  return (
    <ThemeContext.Provider value={value}>
      <FluentProvider theme={fluent} className="app-root">
        {children}
      </FluentProvider>
    </ThemeContext.Provider>
  );
}
