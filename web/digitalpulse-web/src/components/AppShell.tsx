import { Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, MenuDivider } from "@fluentui/react-components";
import {
  Alert20Regular,
  Dismiss20Regular,
  Navigation20Regular,
  PanelLeftContract20Regular,
  PanelLeftExpand20Regular,
  PersonCircle20Regular,
  Search20Regular,
  SignOut20Regular,
  Sparkle20Regular,
  WeatherMoon20Regular,
  WeatherSunny20Regular
} from "@fluentui/react-icons";
import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { Link, NavLink, useLocation, useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { AssistantDrawer } from "../design/AssistantDrawer";
import { CommandPalette } from "../design/CommandPalette";
import { DrawerFrame } from "../design/motion";
import { linkState } from "../design/platforms";
import { useSession } from "../state/session";
import { useUi } from "../state/ui";
import { themeCatalog, useTheme } from "../theme";
import { NAV_GROUPS, activeNavItem } from "./navigation";

const DEV_THEMES = new Set(["editorial", "executive", "future-ai", "minimal"]);

export function AppShell({ children }: { children: React.ReactNode }) {
  const { scheme, setScheme, id: themeId, setId: setThemeId } = useTheme();
  const profile = useSession((s) => s.profile);
  const clear = useSession((s) => s.clear);
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const navOpen = useUi((s) => s.navOpen);
  const setNav = useUi((s) => s.setNav);
  const collapsed = useUi((s) => s.navCollapsed);
  const setCollapsed = useUi((s) => s.setNavCollapsed);
  const setCommand = useUi((s) => s.setCommand);
  const setAssistant = useUi((s) => s.setAssistant);
  const setNotice = useUi((s) => s.setNotice);
  const noticeOpen = useUi((s) => s.noticeOpen);
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const firstId = businesses.data?.[0]?.id;
  const identity = useQuery({
    queryKey: ["identity", firstId],
    queryFn: () => api.identity(firstId!),
    enabled: Boolean(firstId)
  });
  const connections = useQuery({
    queryKey: ["connections", firstId],
    queryFn: () => api.connections(firstId!),
    enabled: Boolean(firstId)
  });
  const ready = useQuery({ queryKey: ["ready"], queryFn: api.ready, retry: false });

  useEffect(() => setNav(false), [pathname, setNav]);

  const states = (connections.data?.connections ?? []).map(linkState);
  const pulse = states.includes("failing")
    ? { tone: "failing", label: "Link failing" }
    : states.includes("warning")
      ? { tone: "warning", label: "Needs reauth" }
      : states.includes("live")
        ? { tone: "live", label: `${states.filter((s) => s === "live").length} linked` }
        : { tone: "idle", label: "No links" };
  const current = activeNavItem(pathname);
  const insights = buildInsights(identity.data);
  const tenantHint = profile?.tenantId ? profile.tenantId.replace(/-/g, "").slice(0, 8) : "no-tenant";
  const holds = (ready.data?.holds ?? []).filter((hold): hold is string => Boolean(hold));
  const vaultHeld = holds.some((hold) => /key vault/i.test(hold));
  const linkedCount = states.filter((s) => s === "live").length;

  return (
    <div className={collapsed ? "os is-collapsed" : "os"}>
      <a className="skip-link" href="#dp-main">Skip to content</a>
      {navOpen ? <button type="button" className="os-scrim" aria-label="Close navigation" onClick={() => setNav(false)} /> : null}
      <aside className={navOpen ? "os-nav is-open" : "os-nav"} aria-label="Product">
        <div className="os-nav-top">
          <Link to="/app" className="mark os-mark" aria-label="DigitalPulse overview"><i /><span>DigitalPulse</span></Link>
          <button type="button" className="icon-btn os-nav-close" aria-label="Close navigation" onClick={() => setNav(false)}>
            <Dismiss20Regular />
          </button>
        </div>
        <nav className="os-nav-groups">
          {NAV_GROUPS.map((group) => (
            <div className="os-nav-group" key={group.label}>
              <p className="os-nav-label">{group.label}</p>
              {group.items.map((item) => {
                const Icon = item.icon;
                const on = current.to === item.to;
                return (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.end}
                    className={on ? "os-nav-link is-on" : "os-nav-link"}
                    aria-current={on ? "page" : undefined}
                    title={collapsed ? item.label : undefined}
                  >
                    <Icon aria-hidden="true" />
                    <span>{item.label}</span>
                  </NavLink>
                );
              })}
            </div>
          ))}
        </nav>
        <div className="os-nav-foot">
          <div className="os-tenant">
            <span className="os-tenant-name">{profile?.tenantName ?? "Workspace"}</span>
            <span className="os-tenant-type">{profile?.tenantType ?? ""}</span>
          </div>
          <button
            type="button"
            className="icon-btn os-collapse"
            aria-label={collapsed ? "Expand navigation" : "Collapse navigation"}
            aria-pressed={collapsed}
            onClick={() => setCollapsed(!collapsed)}
          >
            {collapsed ? <PanelLeftExpand20Regular /> : <PanelLeftContract20Regular />}
          </button>
        </div>
      </aside>

      <header className="os-head">
        <div className="os-head-lead">
          <button type="button" className="icon-btn os-menu" aria-label="Open navigation" aria-expanded={navOpen} onClick={() => setNav(true)}>
            <Navigation20Regular />
          </button>
          <p className="os-crumb" aria-live="polite">{current.label}</p>
        </div>
        <button type="button" className="search-hit" onClick={() => setCommand(true)}>
          <Search20Regular aria-hidden="true" />
          <span>Jump</span>
          <kbd>Ctrl K</kbd>
        </button>
        <div className="os-head-trail">
          <span className={`pulse-chip is-${pulse.tone}`} title="Authorized platform state. Development grants only — not a live health index.">
            <i aria-hidden="true" />
            <span className="pulse-chip-label">{pulse.label}</span>
          </span>
          {(businesses.data?.length ?? 0) > 1 ? (
            <select
              aria-label="Business"
              className="business-hit"
              value={firstId ?? ""}
              onChange={(event) => navigate(`/app/businesses/${event.target.value}`)}
            >
              {(businesses.data ?? []).map((business) => (
                <option key={business.id} value={business.id}>{business.name}</option>
              ))}
            </select>
          ) : (
            <span className="business-name">{businesses.data?.[0]?.name ?? "No business"}</span>
          )}
          <button type="button" className="icon-btn" aria-label="Open AI brief" onClick={() => setAssistant(true)}>
            <Sparkle20Regular />
          </button>
          <button type="button" className="icon-btn" aria-label="Open signals" aria-pressed={noticeOpen} onClick={() => setNotice(!noticeOpen)}>
            <Alert20Regular />
          </button>
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <button type="button" className="icon-btn" aria-label="Account and display">
                <PersonCircle20Regular />
              </button>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem disabled>{profile?.displayName ?? profile?.email ?? "Account"}</MenuItem>
                <MenuItem disabled>
                  {profile?.tenantName ?? "Workspace"} · {linkedCount} linked · {tenantHint}
                </MenuItem>
                <MenuItem disabled>
                  Development JWT · {vaultHeld ? "Key Vault hold" : "Key Vault unconfigured"} · Idempotent
                </MenuItem>
                <MenuDivider />
                <MenuItem
                  icon={scheme === "dark" ? <WeatherSunny20Regular /> : <WeatherMoon20Regular />}
                  onClick={() => setScheme(scheme === "dark" ? "light" : "dark")}
                >
                  {scheme === "dark" ? "Light scheme" : "Dark scheme"}
                </MenuItem>
                {import.meta.env.DEV
                  ? themeCatalog.filter((theme) => DEV_THEMES.has(theme.id)).map((theme) => (
                      <MenuItem key={theme.id} onClick={() => setThemeId(theme.id)}>
                        {theme.label}{theme.id === themeId ? " · on" : ""}
                      </MenuItem>
                    ))
                  : null}
                <MenuDivider />
                <MenuItem
                  icon={<SignOut20Regular />}
                  onClick={async () => {
                    await api.logout().catch(() => undefined);
                    clear();
                    navigate("/");
                  }}
                >
                  Sign out
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
      </header>

      <main className="os-main" id="dp-main">{children}</main>
      <CommandPalette />
      <AssistantDrawer insights={insights} />
      <DrawerFrame open={noticeOpen} label="Signals">
        <div className="drawer-head">
          <p className="hero-kicker kicker-flush">Signals</p>
          <button type="button" className="icon-btn" aria-label="Close signals" onClick={() => setNotice(false)}><Dismiss20Regular /></button>
        </div>
        <div className="dp-empty">
          <strong>Live monitoring is not running</strong>
          <p>Signals from DigitalPulse Check live on the Signals page. Continuous monitoring starts once provider APIs are live.</p>
        </div>
        <Link className="text-link" to="/app/findings" onClick={() => setNotice(false)}>Open signals</Link>
      </DrawerFrame>
    </div>
  );
}

function buildInsights(data: Awaited<ReturnType<typeof api.identity>> | undefined) {
  if (!data) return [];
  const items: { title: string; why: string; action?: string }[] = [];
  if (!data.business.website) items.push({ title: "Website missing", why: "Later scans compare listings to the official site.", action: "Add a website on Profile." });
  if (!data.contacts.length) items.push({ title: "No contact points", why: "Phone and email are the NAP source of truth.", action: "Add a contact." });
  if (!data.facts.some((fact) => fact.status === "Approved")) items.push({ title: "No approved facts", why: "Restricted or draft claims cannot be published.", action: "Approve a fact you can stand behind." });
  if (!data.categories.length) items.push({ title: "Unclassified business", why: "Directories need a category to place the listing.", action: "Add a category." });
  return items;
}
