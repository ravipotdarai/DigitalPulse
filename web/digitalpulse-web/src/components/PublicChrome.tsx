import { Button } from "../design/Button";
import { WeatherMoon20Regular, WeatherSunny20Regular } from "@fluentui/react-icons";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ThemeSwitcher, useTheme } from "../theme";

export function PublicChrome({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { scheme, setScheme } = useTheme();
  const quiet = pathname.startsWith("/onboarding");

  return (
    <div className="relative min-h-dvh">
      <header className="public-bar">
        <Link to="/" className="mark"><i /><span>DigitalPulse</span></Link>
        <nav className="public-nav">
          <ThemeSwitcher />
          <Button
            appearance="subtle"
            icon={scheme === "dark" ? <WeatherSunny20Regular /> : <WeatherMoon20Regular />}
            onClick={() => setScheme(scheme === "dark" ? "light" : "dark")}
            aria-label={scheme === "dark" ? "Switch to light scheme" : "Switch to dark scheme"}
          />
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
