import { Button } from "@fluentui/react-components";
import { WeatherMoon20Regular, WeatherSunny20Regular } from "@fluentui/react-icons";
import { useQuery } from "@tanstack/react-query";
import { Link, NavLink, useLocation, useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { AssistantDrawer } from "../design/AssistantDrawer";
import { CommandPalette } from "../design/CommandPalette";
import { useSession } from "../state/session";
import { useUi } from "../state/ui";

const NAV = [
  { to: "/app", label: "Command", end: true },
  { to: "/app/identity", label: "Identity" },
  { to: "/app/connections", label: "Connections" },
  { to: "/app/findings", label: "Findings" },
  { to: "/app/website", label: "Website" },
  { to: "/app/social", label: "Social" },
  { to: "/app/info", label: "Workspace" }
];

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
  const { pathname } = useLocation();
  const navOpen = useUi((s) => s.navOpen);
  const setNav = useUi((s) => s.setNav);
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
  const linked = connections.data?.connections ?? [];
  const healthLabel = linked.some((item) => item.status === "NeedsReauth")
    ? "Reauth"
    : linked.some((item) => item.status === "Connected")
      ? "Granted"
      : "Offline";

  const insights = buildInsights(identity.data);

  return (
    <div className="os">
      <aside className={navOpen ? "os-nav is-open" : "os-nav"} aria-label="Product">
        <Link to="/app" className="mark" style={{ margin: "0 0.4rem 1rem" }}><i /><span>DigitalPulse</span></Link>
        {NAV.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) => (isActive || (item.to === "/app/identity" && pathname.includes("/businesses/")) ? "is-on" : undefined)}
            onClick={() => setNav(false)}
          >
            {item.label}
          </NavLink>
        ))}
        <div style={{ marginTop: "auto", padding: "1rem 0.7rem 0.4rem", color: "var(--muted)", fontSize: "0.75rem" }}>
          {profile?.tenantName ?? "Workspace"}
          <br />
          {profile?.tenantType ?? ""}
        </div>
      </aside>

      <header className="os-head">
        <Button appearance="subtle" className="os-menu" onClick={() => setNav(!navOpen)} aria-label="Open navigation">Menu</Button>
        <button type="button" className="search-hit" onClick={() => setCommand(true)}>
          <span>Search the OS</span>
          <kbd>Ctrl K</kbd>
        </button>
        <span className="health-pill" title="Development grants only. Production provider APIs are not configured.">
          <i style={{ background: healthLabel === "Granted" ? "var(--ok)" : healthLabel === "Reauth" ? "var(--warn)" : undefined }} />
          {healthLabel}
        </span>
        <select
          aria-label="Business"
          className="search-hit"
          style={{ flex: "0 1 12rem" }}
          value={businesses.data?.[0]?.id ?? ""}
          onChange={(event) => navigate(`/app/businesses/${event.target.value}`)}
        >
          {(businesses.data ?? []).map((business) => (
            <option key={business.id} value={business.id}>{business.name}</option>
          ))}
          {!businesses.data?.length ? <option value="">No business</option> : null}
        </select>
        <Button appearance="subtle" onClick={() => setAssistant(true)}>Insights</Button>
        <Button appearance="subtle" onClick={() => setNotice(!noticeOpen)}>Signals</Button>
        <Button appearance="subtle" icon={dark ? <WeatherSunny20Regular /> : <WeatherMoon20Regular />} onClick={onToggleTheme} aria-label="Toggle theme" />
        <Button appearance="subtle" onClick={async () => { await api.logout().catch(() => undefined); clear(); navigate("/"); }}>
          Sign out
        </Button>
      </header>

      <main className="os-main">{children}</main>
      <CommandPalette />
      <AssistantDrawer insights={insights} />
      {noticeOpen ? (
        <aside className="drawer" aria-label="Signals">
          <div className="flex items-center justify-between">
            <p className="hero-kicker" style={{ margin: 0 }}>Signals</p>
            <Button appearance="subtle" onClick={() => setNotice(false)}>Close</Button>
          </div>
          <div className="dp-empty">
            <strong>No signals yet</strong>
            <p>Monitoring starts after platforms are connected and authorized.</p>
          </div>
        </aside>
      ) : null}
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
