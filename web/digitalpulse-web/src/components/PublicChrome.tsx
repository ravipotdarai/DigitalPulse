import { Button } from "@fluentui/react-components";
import { WeatherMoon20Regular, WeatherSunny20Regular } from "@fluentui/react-icons";
import { Link, useLocation, useNavigate } from "react-router-dom";

export function PublicChrome({
  children,
  dark,
  onToggleTheme
}: {
  children: React.ReactNode;
  dark: boolean;
  onToggleTheme: () => void;
}) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const quiet = pathname.startsWith("/onboarding");

  return (
    <div className="relative min-h-dvh">
      <header className="public-bar">
        <Link to="/" className="mark"><i /><span>DigitalPulse</span></Link>
        <nav className="flex flex-wrap items-center justify-end gap-2">
          <Button appearance="subtle" icon={dark ? <WeatherSunny20Regular /> : <WeatherMoon20Regular />} onClick={onToggleTheme} aria-label="Toggle theme" />
          {quiet ? null : (
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
