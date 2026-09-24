import {
  BookOpen20Regular,
  BuildingShop20Regular,
  Folder20Regular,
  Globe20Regular,
  Megaphone20Regular,
  PlugConnected20Regular,
  Pulse20Regular,
  Radar20Regular,
  Settings20Regular,
  PeopleTeam20Regular,
  Chat20Regular,
  Flash20Regular,
  Sparkle20Regular,
  Eye20Regular
} from "@fluentui/react-icons";
import type { FluentIcon } from "@fluentui/react-icons";

export type NavItem = { to: string; label: string; icon: FluentIcon; end?: boolean; matches?: string[] };
export type NavGroup = { label: string; items: NavItem[] };

export const NAV_GROUPS: NavGroup[] = [
  {
    label: "Command",
    items: [
      { to: "/app", label: "Overview", icon: Pulse20Regular, end: true },
      { to: "/app/findings", label: "Signals", icon: Radar20Regular },
      { to: "/app/ai", label: "Orchestrator", icon: Sparkle20Regular },
      { to: "/app/actions", label: "Actions", icon: Flash20Regular },
      { to: "/app/monitoring", label: "Monitor", icon: Eye20Regular }
    ]
  },
  {
    label: "Business",
    items: [
      { to: "/app/identity", label: "Identity", icon: BookOpen20Regular, matches: ["/app/businesses/"] },
      { to: "/app/connections", label: "Connections", icon: PlugConnected20Regular },
      { to: "/app/projects", label: "Projects", icon: Folder20Regular }
    ]
  },
  {
    label: "Growth",
    items: [
      { to: "/app/website", label: "Website & search", icon: Globe20Regular },
      { to: "/app/social", label: "Social", icon: Megaphone20Regular },
      { to: "/app/whatsapp", label: "WhatsApp", icon: Chat20Regular },
      { to: "/app/directories", label: "Directories", icon: BuildingShop20Regular }
    ]
  },
  {
    label: "System",
    items: [
      { to: "/app/agency", label: "Agency", icon: PeopleTeam20Regular },
      { to: "/app/operations", label: "Operations", icon: Settings20Regular },
      { to: "/app/billing", label: "Billing", icon: Settings20Regular },
      { to: "/app/info", label: "Workspace", icon: Settings20Regular }
    ]
  }
];

export function activeNavItem(pathname: string) {
  const items = NAV_GROUPS.flatMap((group) => group.items);
  return (
    items.find((item) => item.matches?.some((prefix) => pathname.startsWith(prefix))) ??
    items.find((item) => (item.end ? pathname === item.to : pathname.startsWith(item.to) && item.to !== "/app")) ??
    items[0]
  );
}
