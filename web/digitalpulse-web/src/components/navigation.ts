import {
  BuildingShop20Regular,
  Folder20Regular,
  Globe20Regular,
  Home20Regular,
  Person20Regular,
  Radar20Regular,
  PeopleTeam20Regular,
  Chat20Regular,
  Flash20Regular,
  Sparkle20Regular,
  Eye20Regular,
  WalletCreditCard20Regular,
  Briefcase20Regular,
  BuildingMultiple20Regular,
  ShareAndroid20Regular,
  Video20Regular,
  PlugConnected20Regular,
  CalendarLtr20Regular,
  Wrench20Regular
} from "@fluentui/react-icons";
import type { FluentIcon } from "@fluentui/react-icons";

export type NavItem = {
  to: string;
  label: string;
  icon: FluentIcon;
  brand?: string;
  end?: boolean;
  matches?: string[];
  search?: string;
  children?: NavItem[];
};

export type NavGroup = { label: string; icon: FluentIcon; items: NavItem[] };

export const NAV_GROUPS: NavGroup[] = [
  {
    label: "Business",
    icon: BuildingMultiple20Regular,
    items: [
      { to: "/app", label: "Overview", icon: Home20Regular, end: true },
      { to: "/app/info", label: "Workspace", icon: Briefcase20Regular },
      { to: "/app/billing", label: "Billing", icon: WalletCreditCard20Regular },
      { to: "/app/identity", label: "Profile", icon: Person20Regular, matches: ["/app/businesses/"] },
      { to: "/app/agency", label: "Clients", icon: PeopleTeam20Regular },
      { to: "/app/website", label: "Website & search", icon: Globe20Regular },
      {
        to: "/app/monitoring",
        label: "Monitor",
        icon: Eye20Regular,
        children: [{ to: "/app/findings", label: "Scans", icon: Radar20Regular }]
      },
      { to: "/app/projects", label: "Projects", icon: Folder20Regular }
    ]
  },
  {
    label: "Social",
    icon: ShareAndroid20Regular,
    items: [
      { to: "/app/social", label: "Common post", icon: CalendarLtr20Regular, end: true },
      { to: "/app/connections", label: "Connection center", icon: PlugConnected20Regular },
      { to: "/app/social/google", label: "Google", icon: Globe20Regular, brand: "GOOGLE" },
      { to: "/app/whatsapp", label: "WhatsApp", icon: Chat20Regular, brand: "WHATSAPP" },
      { to: "/app/social/facebook", label: "Facebook", icon: ShareAndroid20Regular, brand: "FACEBOOK" },
      { to: "/app/social/instagram", label: "Instagram", icon: ShareAndroid20Regular, brand: "INSTAGRAM" },
      { to: "/app/social/linkedin", label: "LinkedIn", icon: Briefcase20Regular, brand: "LINKEDIN" },
      { to: "/app/social/youtube", label: "YouTube", icon: Video20Regular, brand: "YOUTUBE" }
    ]
  },
  {
    label: "Growth",
    icon: BuildingShop20Regular,
    items: [
      { to: "/app/directories", label: "IndiaMART", icon: BuildingShop20Regular, brand: "INDIAMART", search: "platform=INDIAMART" },
      { to: "/app/directories", label: "Justdial", icon: BuildingShop20Regular, brand: "JUSTDIAL", search: "platform=JUSTDIAL" }
    ]
  },
  {
    label: "Desk",
    icon: Sparkle20Regular,
    items: [
      { to: "/app/ai", label: "Orchestrator", icon: Sparkle20Regular },
      { to: "/app/actions", label: "Actions", icon: Flash20Regular },
      { to: "/app/operations", label: "Operations", icon: Wrench20Regular }
    ]
  }
];

export function navHref(item: NavItem) {
  return item.search ? `${item.to}?${item.search}` : item.to;
}

export function flattenNav(groups: NavGroup[] = NAV_GROUPS): NavItem[] {
  const items: NavItem[] = [];
  const walk = (item: NavItem) => {
    items.push(item);
    item.children?.forEach(walk);
  };
  groups.forEach((group) => group.items.forEach(walk));
  return items;
}

function pathOf(item: NavItem) {
  return item.to.split("?")[0];
}

export function itemIsOn(item: NavItem, pathname: string, search = "") {
  const query = search.replace(/^\?/, "");
  const path = pathOf(item);
  if (item.search) return pathname === path && query.includes(item.search);
  if (item.matches?.some((prefix) => pathname.startsWith(prefix))) return true;
  if (item.end) return pathname === item.to && !query.includes("pane=") && !query.includes("platform=");
  return pathname === path || (pathname.startsWith(`${path}/`) && path !== "/app");
}

export function branchIsOpen(item: NavItem, pathname: string, search = "") {
  if (!item.children?.length) return false;
  if (itemIsOn(item, pathname, search)) return true;
  return item.children.some((child) => itemIsOn(child, pathname, search) || pathname.startsWith(pathOf(child)));
}

export function groupIsOn(group: NavGroup, pathname: string, search = "") {
  return group.items.some((item) => itemIsOn(item, pathname, search) || branchIsOpen(item, pathname, search));
}

export function activeNavItem(pathname: string, search = "") {
  const items = flattenNav();
  return (
    items.find((item) => item.search && itemIsOn(item, pathname, search)) ??
    items.find((item) => item.matches?.some((prefix) => pathname.startsWith(prefix))) ??
    items.find((item) => !item.search && !item.children && itemIsOn(item, pathname, search)) ??
    items.find((item) => !item.search && itemIsOn(item, pathname, search)) ??
    items[0]
  );
}
