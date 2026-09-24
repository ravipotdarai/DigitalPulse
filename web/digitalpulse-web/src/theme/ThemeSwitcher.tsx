import { useTheme } from "./ThemeContext";
import { themeCatalog } from "./registry";

const SWITCHABLE = new Set(["editorial", "executive", "future-ai", "minimal"]);

/** Developer-only visual switcher. Does not touch tenant or plan logic. */
export function ThemeSwitcher() {
  const { id, setId } = useTheme();
  if (!import.meta.env.DEV) return null;

  return (
    <div className="theme-switcher" role="group" aria-label="Developer theme">
      <label className="sr-only" htmlFor="dp-theme-id">Theme</label>
      <select
        id="dp-theme-id"
        value={SWITCHABLE.has(id) ? id : "editorial"}
        onChange={(event) => setId(event.target.value as typeof id)}
      >
        {themeCatalog.filter((theme) => SWITCHABLE.has(theme.id)).map((theme) => (
          <option key={theme.id} value={theme.id}>
            {theme.label}
          </option>
        ))}
      </select>
    </div>
  );
}
