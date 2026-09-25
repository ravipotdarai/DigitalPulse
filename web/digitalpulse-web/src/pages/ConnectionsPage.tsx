import { Button } from "../design/Button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, motion } from "framer-motion";
import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiError, api, type ConnectionDiagnostic, type PlatformCatalogItem, type PlatformConnection } from "../lib/api";
import { PageState } from "../components/PageState";
import { panelTransition, useMotionTiming } from "../design/motion";
import { Meter, PageHeader } from "../design/PageHeader";
import { PlatformEcosystem } from "../design/PlatformEcosystem";
import { BrandMark } from "../design/BrandMark";
import { LINK_LABEL, linkState, relativeTime } from "../design/platforms";

export function ConnectionsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["connections", businessId],
    queryFn: () => api.connections(businessId!),
    enabled: Boolean(businessId)
  });
  const [params] = useSearchParams();

  if (businesses.isLoading) return <PageState mode="loading" title="Opening connection center" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before connecting platforms." />;
  if (query.isLoading) return <PageState mode="loading" title="Mapping platforms" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Connection center unavailable" />;

  return (
    <ConnectionCenter
      businessId={businessId}
      businessName={businesses.data?.[0]?.name ?? "Business"}
      catalog={query.data.catalog}
      connections={query.data.connections}
      maxConnections={query.data.maxConnections}
      justConnected={params.get("connected")}
      focus={params.get("platform")}
    />
  );
}

function ConnectionCenter({
  businessId,
  businessName,
  catalog,
  connections,
  maxConnections,
  justConnected,
  focus
}: {
  businessId: string;
  businessName: string;
  catalog: PlatformCatalogItem[];
  connections: PlatformConnection[];
  maxConnections: number;
  justConnected: string | null;
  focus: string | null;
}) {
  const queryClient = useQueryClient();
  const { reduce, base } = useMotionTiming();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [diagnostics, setDiagnostics] = useState<{ code: string; items: ConnectionDiagnostic[] } | null>(null);
  const byCode = useMemo(() => new Map(connections.map((item) => [item.platformCode, item])), [connections]);
  const [selected, setSelected] = useState<string>(
    () => focus ?? justConnected ?? connections[0]?.platformCode ?? catalog[0]?.code ?? ""
  );
  const platform = catalog.find((item) => item.code === selected) ?? catalog[0];
  const connection = platform ? byCode.get(platform.code) : undefined;
  const state = linkState(connection);

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

  async function run(label: string, action: () => Promise<unknown>) {
    setError(null);
    setBusy(label);
    try {
      await action();
      await queryClient.invalidateQueries({ queryKey: ["connections"] });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "The connection change failed.");
    } finally {
      setBusy(null);
    }
  }

  const caps = platform
    ? [
        ["Read", platform.capabilities.canRead],
        ["Create", platform.capabilities.canCreate],
        ["Update", platform.capabilities.canUpdate],
        ["Publish", platform.capabilities.canPublish],
        ["Metrics", platform.capabilities.canGetMetrics],
        ["Assisted", platform.capabilities.assistedOnly]
      ] as const
    : [];

  return (
    <div className="page-view">
      <PageHeader
        kicker="Connection center"
        title="The ecosystem around the business"
        lead="DigitalPulse sits between the business and every platform it is present on. Each link is an authorized adapter. Development grants prove the contract; live Google, Meta, or WhatsApp APIs are not invented here."
        aside={
          <Meter
            label="Connections on this plan"
            value={connections.length}
            max={maxConnections}
            detail={connections.length >= maxConnections ? "Plan limit reached. Upgrade to add more platforms." : undefined}
          />
        }
      >
        {justConnected ? <p className="note-ok" role="status">{justConnected} is connected with a development grant.</p> : null}
        {error ? <p className="note-err" role="alert">{error}</p> : null}
      </PageHeader>

      <div className="conn">
        <section aria-label="Platform ecosystem">
          <PlatformEcosystem
            centerLabel={businessName}
            centerMeta={`${connections.length} of ${maxConnections}`}
            selected={platform?.code}
            onSelect={(code) => {
              setSelected(code);
              setDiagnostics(null);
            }}
            nodes={catalog.map((item) => {
              const link = byCode.get(item.code);
              return {
                code: item.code,
                name: item.name,
                category: item.category,
                state: linkState(link),
                meta: link?.lastHealthStatus ?? null,
                detail: item.summary
              };
            })}
          />
          <div className="conn-list" role="group" aria-label="Platforms">
            {catalog.map((item) => {
              const link = byCode.get(item.code);
              const itemState = linkState(link);
              return (
                <button
                  key={item.code}
                  type="button"
                  aria-pressed={item.code === platform?.code}
                  onClick={() => {
                    setSelected(item.code);
                    setDiagnostics(null);
                  }}
                >
                  <BrandMark code={item.code} name={item.name} className={`is-${itemState}`} />
                  <span className="signal-text">
                    <span className="signal-title">{item.name}</span>
                    <span className="signal-meta">{item.category} · {LINK_LABEL[itemState]}</span>
                  </span>
                </button>
              );
            })}
          </div>
        </section>

        <AnimatePresence mode="wait">
          {platform ? (
            <motion.aside
              key={platform.code}
              className="conn-detail"
              aria-label={`${platform.name} details`}
              {...panelTransition(reduce, base)}
            >
              <div>
                <p className="hero-kicker">{platform.category} · {platform.authMode}</p>
                <h2>{platform.name}</h2>
              </div>
              <span className={`conn-state is-${state}`}><i aria-hidden="true" />{LINK_LABEL[state]}</span>
              <p>{platform.summary}</p>

              <dl className="facts">
                <div><dt>Account</dt><dd>{connection?.externalAccount ?? "—"}</dd></div>
                <div><dt>Grant</dt><dd>{connection?.grantKind ?? "—"}</dd></div>
                <div><dt>Connected</dt><dd>{relativeTime(connection?.connectedAtUtc) ?? "—"}</dd></div>
                <div><dt>Last health check</dt><dd>{connection?.lastHealthStatus ? `${connection.lastHealthStatus} · ${relativeTime(connection.lastHealthAtUtc)}` : "—"}</dd></div>
                {connection?.lastError ? <div><dt>Last error</dt><dd className="note-err">{connection.lastError}</dd></div> : null}
              </dl>

              <div>
                <p className="section-kicker">Adapter capabilities</p>
                <div className="caps">
                  {caps.map(([label, on]) => (
                    <span key={label} className={on ? "cap is-on" : "cap"}>{label}</span>
                  ))}
                </div>
              </div>

              <div className="conn-actions">
                {!connection ? (
                  <Button
                    appearance="primary"
                    disabled={start.isPending || connections.length >= maxConnections}
                    onClick={() => start.mutate(platform.code)}
                  >
                    {start.isPending ? "Authorizing…" : platform.authMode === "Assisted" ? "Enable assisted" : "Connect"}
                  </Button>
                ) : (
                  <>
                    <Button appearance="primary" disabled={busy !== null} onClick={() => void run("health", () => api.healthConnection(businessId, connection.id))}>
                      {busy === "health" ? "Checking…" : "Run health check"}
                    </Button>
                    <Button
                      appearance="subtle"
                      disabled={busy !== null}
                      onClick={async () => {
                        setError(null);
                        setBusy("diagnose");
                        try {
                          setDiagnostics({ code: platform.code, items: await api.diagnoseConnection(businessId, connection.id) });
                        } catch (err) {
                          setError(err instanceof ApiError ? err.title : "Diagnostics failed.");
                        } finally {
                          setBusy(null);
                        }
                      }}
                    >
                      {busy === "diagnose" ? "Diagnosing…" : "Diagnose"}
                    </Button>
                    <Button
                      appearance="subtle"
                      disabled={busy !== null}
                      onClick={async () => {
                        const result = await api.reauthorizeConnection(businessId, connection.id);
                        if (result.authorizationUrl) window.location.assign(result.authorizationUrl);
                        else await queryClient.invalidateQueries({ queryKey: ["connections"] });
                      }}
                    >
                      Reauthorize
                    </Button>
                    <Button appearance="subtle" disabled={busy !== null} onClick={() => void run("disconnect", () => api.disconnectConnection(businessId, connection.id))}>
                      Disconnect
                    </Button>
                  </>
                )}
              </div>

              {diagnostics?.code === platform.code ? (
                <div>
                  <p className="section-kicker">Diagnostics</p>
                  <div className="diag">
                    {diagnostics.items.map((item) => (
                      <div key={item.check}>
                        <span>
                          {item.check}
                          <small>{item.detail}</small>
                        </span>
                        <span className={`sev ${item.status === "Pass" ? "sev-ok" : item.status === "Fail" ? "sev-crit" : "sev-hold"}`}>{item.status}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ) : null}
            </motion.aside>
          ) : null}
        </AnimatePresence>
      </div>
    </div>
  );
}
