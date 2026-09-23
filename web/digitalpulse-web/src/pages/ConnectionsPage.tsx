import { Button } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiError, api, type ConnectionDiagnostic, type PlatformCatalogItem, type PlatformConnection } from "../lib/api";
import { PageState } from "../components/PageState";

export function ConnectionsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["connections", businessId],
    queryFn: () => api.connections(businessId!),
    enabled: Boolean(businessId)
  });
  const [params] = useSearchParams();
  const connected = params.get("connected");

  if (businesses.isLoading) return <PageState mode="loading" title="Opening connection center" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before connecting platforms." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading platforms" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Connection center unavailable" />;

  return (
    <ConnectionCenter
      businessId={businessId}
      catalog={query.data.catalog}
      connections={query.data.connections}
      maxConnections={query.data.maxConnections}
      justConnected={connected}
    />
  );
}

function ConnectionCenter({
  businessId,
  catalog,
  connections,
  maxConnections,
  justConnected
}: {
  businessId: string;
  catalog: PlatformCatalogItem[];
  connections: PlatformConnection[];
  maxConnections: number;
  justConnected: string | null;
}) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [diagnostics, setDiagnostics] = useState<{ name: string; items: ConnectionDiagnostic[] } | null>(null);
  const byCode = useMemo(() => new Map(connections.map((item) => [item.platformCode, item])), [connections]);

  const start = useMutation({
    mutationFn: (code: string) => api.startConnection(businessId, code),
    onSuccess: async (result) => {
      setError(null);
      if (result.authorizationUrl) {
        window.location.assign(result.authorizationUrl);
        return;
      }
      await queryClient.invalidateQueries({ queryKey: ["connections"] });
    },
    onError: (err) => setError(err instanceof ApiError ? err.title : "Could not start the connection.")
  });

  async function run(action: () => Promise<unknown>) {
    setError(null);
    try {
      await action();
      await queryClient.invalidateQueries({ queryKey: ["connections"] });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "The connection change failed.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Connection center</p>
        <h1 className="display" style={{ fontSize: "clamp(2rem, 5vw, 3.4rem)", margin: 0 }}>Authorized platforms</h1>
        <p style={{ color: "var(--muted)", maxWidth: "38rem" }}>
          {connections.length} / {maxConnections} connections on this plan. Development grants prove the adapter contract. Live Google, Meta, or WhatsApp APIs are not invented here.
        </p>
        {justConnected ? <p className="note-ok">{justConnected} is connected with a development grant.</p> : null}
        {error ? <p className="note-err" role="alert">{error}</p> : null}
      </header>

      <div className="band band-3">
        {catalog.map((platform) => {
          const connection = byCode.get(platform.code);
          return (
            <article className="panel" key={platform.code}>
              <p className="hero-kicker">{platform.category} · {platform.authMode}</p>
              <h3>{platform.name}</h3>
              <p style={{ color: "var(--muted)", margin: "0 0 0.7rem" }}>{platform.summary}</p>
              <div className="row-line">
                <span>Status</span>
                <span className={`sev ${tone(connection?.status)}`}>{connection?.status ?? "Not connected"}</span>
              </div>
              {connection?.externalAccount ? (
                <div className="row-line"><span>Account</span><span>{connection.externalAccount}</span></div>
              ) : null}
              {connection?.lastHealthStatus ? (
                <div className="row-line"><span>Health</span><span className={`sev ${tone(connection.lastHealthStatus)}`}>{connection.lastHealthStatus}</span></div>
              ) : null}
              <p style={{ fontSize: "0.75rem", color: "var(--muted)" }}>
                {[
                  platform.capabilities.canRead && "Read",
                  platform.capabilities.canCreate && "Create",
                  platform.capabilities.canUpdate && "Update",
                  platform.capabilities.canPublish && "Publish",
                  platform.capabilities.canGetMetrics && "Metrics",
                  platform.capabilities.assistedOnly && "Assisted"
                ].filter(Boolean).join(" · ")}
              </p>
              <div className="id-form-actions" style={{ marginTop: "0.7rem" }}>
                {!connection ? (
                  <Button appearance="primary" disabled={start.isPending} onClick={() => start.mutate(platform.code)}>
                    {platform.authMode === "Assisted" ? "Enable assisted" : "Connect"}
                  </Button>
                ) : (
                  <>
                    <Button appearance="subtle" onClick={() => { void run(() => api.healthConnection(businessId, connection.id)); }}>Health</Button>
                    <Button appearance="subtle" onClick={async () => {
                      setError(null);
                      try {
                        const items = await api.diagnoseConnection(businessId, connection.id);
                        setDiagnostics({ name: platform.name, items });
                      } catch (err) {
                        setError(err instanceof ApiError ? err.title : "Diagnostics failed.");
                      }
                    }}>Diagnose</Button>
                    <Button appearance="subtle" onClick={async () => {
                      const result = await api.reauthorizeConnection(businessId, connection.id);
                      if (result.authorizationUrl) window.location.assign(result.authorizationUrl);
                      else await queryClient.invalidateQueries({ queryKey: ["connections"] });
                    }}>Reauthorize</Button>
                    <Button appearance="subtle" onClick={() => { void run(() => api.disconnectConnection(businessId, connection.id)); }}>Disconnect</Button>
                  </>
                )}
              </div>
            </article>
          );
        })}
      </div>

      {diagnostics ? (
        <aside className="panel">
          <div className="flex items-center justify-between gap-3">
            <h2>Diagnostics · {diagnostics.name}</h2>
            <Button appearance="subtle" onClick={() => setDiagnostics(null)}>Close</Button>
          </div>
          {diagnostics.items.map((item) => (
            <div className="row-line" key={item.check}>
              <div>
                <strong>{item.check}</strong>
                <p style={{ margin: "0.2rem 0 0", color: "var(--muted)" }}>{item.detail}</p>
              </div>
              <span className={`sev ${item.status === "Pass" ? "sev-ok" : item.status === "Fail" ? "sev-crit" : "sev-hold"}`}>{item.status}</span>
            </div>
          ))}
        </aside>
      ) : null}
    </section>
  );
}

function tone(status?: string) {
  if (status === "Connected" || status === "Healthy") return "sev-ok";
  if (status === "NeedsReauth" || status === "Connecting") return "sev-warn";
  if (status === "Error") return "sev-crit";
  return "sev-hold";
}
