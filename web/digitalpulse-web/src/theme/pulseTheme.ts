/** @deprecated Use ThemeProvider + fluentFromTokens. Kept so existing imports compile. */
import { fluentFromTokens } from "./fluentFromTokens";
import { editorialTheme } from "./themes/editorial";

export const lightTheme = fluentFromTokens(editorialTheme, "light");
export const darkTheme = fluentFromTokens(editorialTheme, "dark");
