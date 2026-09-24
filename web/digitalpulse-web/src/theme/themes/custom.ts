import type { ThemeTokens } from "../types";
import { editorialTheme } from "./editorial";

/** Reserved slot for a tenant-authored theme. Falls back to Editorial until configured. */
export const customTheme: ThemeTokens = {
  ...editorialTheme,
  id: "custom",
  label: "Custom",
  summary: "Placeholder for a later tenant-authored visual language."
};
