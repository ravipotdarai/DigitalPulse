import type { PlatformConnection } from "../lib/api";

export type LinkState = "live" | "syncing" | "warning" | "failing" | "idle";

const MONOGRAMS: Record<string, string> = {
  GOOGLE: "G",
  FACEBOOK: "f",
  INSTAGRAM: "Ig",
  LINKEDIN: "in",
  YOUTUBE: "Yt",
  WHATSAPP: "Wa",
  WEBSITE: "W",
  SEARCHCONSOLE: "Sc",
  GOOGLEADS: "Ads",
  INDIAMART: "IM",
  JUSTDIAL: "Jd"
};

export function monogram(code: string, name: string) {
  return MONOGRAMS[code.toUpperCase()] ?? name.slice(0, 2);
}

export function isSignedIn(
  connection?: Pick<PlatformConnection, "status" | "hasLiveCredential" | "grantKind"> | null
) {
  if (!connection || connection.grantKind === "Development") return false;
  return connection.status === "Connected" && (connection.hasLiveCredential || connection.grantKind === "Assisted");
}

export function linkState(connection?: Pick<PlatformConnection, "status" | "lastHealthStatus" | "hasLiveCredential" | "grantKind"> | null): LinkState {
  if (!connection || connection.grantKind === "Development") return "idle";
  if (connection.status === "Error" || connection.lastHealthStatus === "Error" || connection.lastHealthStatus === "Failing") return "failing";
  if (connection.status === "NeedsReauth" || connection.lastHealthStatus === "Degraded") return "warning";
  if (connection.status === "Connecting") return "syncing";
  if (isSignedIn(connection)) return "live";
  if (connection.status === "Connected") return "warning";
  return "idle";
}

export const LINK_LABEL: Record<LinkState, string> = {
  live: "Connected",
  syncing: "Authorizing",
  warning: "Needs attention",
  failing: "Failing",
  idle: "Not connected"
};

export function severityRank(severity: string) {
  switch (severity) {
    case "Critical":
      return 4;
    case "High":
      return 3;
    case "Medium":
      return 2;
    case "Low":
      return 1;
    default:
      return 0;
  }
}

export function severityTone(severity: string) {
  const rank = severityRank(severity);
  if (rank >= 3) return "failing";
  if (rank === 2) return "warning";
  return "idle";
}

export function relativeTime(iso: string | null | undefined) {
  if (!iso) return null;
  const diff = Date.now() - new Date(iso).getTime();
  const minutes = Math.round(diff / 60000);
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.round(hours / 24)}d ago`;
}
