import { Button } from "@fluentui/react-components";
import { WeatherMoon20Regular, WeatherSunny20Regular } from "@fluentui/react-icons";
import { Link, useNavigate } from "react-router-dom";
import { api, getStoredToken } from "../lib/api";
import { useSession } from "../state/session";

export function AppShell({
  children,
  dark,
  onToggleTheme
}: {
  children: React.ReactNode;
  dark: boolean;
  onToggleTheme: () => void;
}) {
  const profile = useSession((s) => s.profile);
  const clear = useSession((s) => s.clear);
  const navigate = useNavigate();
  const signedIn = Boolean(getStoredToken() || profile);

  return (
    <div className="relative min-h-dvh">
      <header className="sticky top-0 z-30 flex items-center justify-between gap-3 border-b px-4 py-3 backdrop-blur md:px-8" style={{ borderColor: "var(--stroke)", background: "color-mix(in srgb, var(--bg) 82%, transparent)" }}>
        <Link to={signedIn ? "/onboarding" : "/"} className="display text-xl tracking-tight">
          DigitalPulse
        </Link>
        <nav className="flex flex-wrap items-center justify-end gap-2">
          <Button appearance="subtle" icon={dark ? <WeatherSunny20Regular /> : <WeatherMoon20Regular />} onClick={onToggleTheme} aria-label="Toggle theme" />
          {signedIn ? (
            <>
              {profile?.tenantId ? (
                <>
                  <Button appearance="subtle" onClick={() => navigate("/app")}>Dashboard</Button>
                  <Button appearance="subtle" onClick={() => navigate("/app/info")}>Update info</Button>
                </>
              ) : null}
              <Button appearance="secondary" onClick={async () => { await api.logout().catch(() => undefined); clear(); navigate("/"); }}>
                Sign out
              </Button>
            </>
          ) : (
            <>
              <Button appearance="subtle" onClick={() => navigate("/login")}>Log in</Button>
              <Button appearance="primary" onClick={() => navigate("/register")}>Start</Button>
            </>
          )}
        </nav>
      </header>
      {children}
    </div>
  );
}
