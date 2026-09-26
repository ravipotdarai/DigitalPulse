import { Button } from "../design/Button";
import { SelectField } from "../design/Field";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, motion } from "framer-motion";
import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiError, api, type ConnectionAccountOption, type ConnectionDiagnostic, type OfficialOAuthApps, type PlatformCatalogItem, type PlatformConnection } from "../lib/api";
import { PageState } from "../components/PageState";
import { panelTransition, useMotionTiming } from "../design/motion";
import { Meter, PageHeader } from "../design/PageHeader";
import { PlatformEcosystem } from "../design/PlatformEcosystem";
import { BrandMark } from "../design/BrandMark";
import { Field } from "../design/Field";
import { isSignedIn, LINK_LABEL, linkState, relativeTime } from "../design/platforms";
import { notifyOAuthOpener, openOAuthWindow, watchOAuthPopup } from "../lib/platformOAuth";

const ACCOUNT_PICKERS = new Set(["FACEBOOK", "INSTAGRAM", "YOUTUBE", "GOOGLE", "SEARCH_CONSOLE", "GOOGLE_ANALYTICS", "LINKEDIN"]);

export function ConnectionsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["connections", businessId],
    queryFn: () => api.connections(businessId!),
    enabled: Boolean(businessId)
  });
  const [params] = useSearchParams();
  const justConnected = params.get("connected");

  useEffect(() => {
    if (justConnected) notifyOAuthOpener(justConnected);
  }, [justConnected]);

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
      officialApps={query.data.officialApps}
      justConnected={justConnected}
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
  officialApps,
  justConnected,
  focus
}: {
  businessId: string;
  businessName: string;
  catalog: PlatformCatalogItem[];
  connections: PlatformConnection[];
  maxConnections: number;
  officialApps: OfficialOAuthApps;
  justConnected: string | null;
  focus: string | null;
}) {
  const queryClient = useQueryClient();
  const { reduce, base } = useMotionTiming();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [appsOpen, setAppsOpen] = useState(() => officialApps.apps.every((app) => !app.ready));
  const [diagnostics, setDiagnostics] = useState<{ code: string; items: ConnectionDiagnostic[] } | null>(null);
  const [accounts, setAccounts] = useState<ConnectionAccountOption[]>([]);
  const [pickedAccount, setPickedAccount] = useState("");
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
      if (result.completeInPlace) {
        await queryClient.invalidateQueries({ queryKey: ["connections"] });
        return;
      }
      if (result.authorizationUrl) {
        const popup = openOAuthWindow(result.authorizationUrl);
        if (!popup) {
          window.location.assign(result.authorizationUrl);
          return;
        }
        await watchOAuthPopup(popup, window.location.origin);
        await queryClient.invalidateQueries({ queryKey: ["connections"] });
        return;
      }
      if (result.needsOfficialApp) {
        setAppsOpen(true);
        setError(`Save official ${platform?.name ?? "platform"} app credentials below, then sign in again. DigitalPulse opens the real ${platform?.name ?? "provider"} login.`);
        return;
      }
      setError(`Sign in to ${platform?.name ?? "this platform"} did not open.`);
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
        lead="Sign in opens the official Google, Meta, or LinkedIn login. Save those official app credentials once on this host, then connect each platform."
        aside={
          <Meter
            label="Connections on this plan"
            value={connections.length}
            max={maxConnections}
            detail={connections.length >= maxConnections ? "Plan limit reached. Upgrade to add more platforms." : undefined}
          />
        }
      >
        {justConnected && isSignedIn(byCode.get(justConnected)) ? (
          <p className="note-ok" role="status">
            {justConnected} is connected
            {byCode.get(justConnected)?.hasLiveCredential
              ? " with an official login. Tokens stay on this host and are refreshed when they expire."
              : byCode.get(justConnected)?.grantKind === "Assisted"
                ? " as an assisted workspace."
                : "."}
          </p>
        ) : null}
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        <OfficialAppsForm
          open={appsOpen}
          apps={officialApps}
          onToggle={() => setAppsOpen((value) => !value)}
          onSaved={() => queryClient.invalidateQueries({ queryKey: ["connections"] })}
        />
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
                <div><dt>Grant</dt><dd>{connection?.grantKind ?? "—"}{connection?.hasLiveCredential ? " · live credential" : ""}</dd></div>
                <div><dt>Connected</dt><dd>{relativeTime(connection?.connectedAtUtc) ?? "—"}</dd></div>
                <div><dt>Last health check</dt><dd>{connection?.lastHealthStatus ? `${connection.lastHealthStatus} · ${relativeTime(connection.lastHealthAtUtc)}` : "—"}</dd></div>
                {connection?.lastError ? <div><dt>Last error</dt><dd className="note-err">{connection.lastError}</dd></div> : null}
              </dl>

              {connection?.hasLiveCredential && ACCOUNT_PICKERS.has(platform.code) ? (
                <div className="conn-actions">
                  <Button
                    appearance="subtle"
                    disabled={busy !== null}
                    onClick={() => void run("accounts", async () => {
                      const items = await api.connectionAccounts(businessId, connection.id);
                      setAccounts(items);
                      setPickedAccount(connection.externalAccount && items.some((item) => item.id === connection.externalAccount)
                        ? connection.externalAccount
                        : items[0]?.id ?? "");
                      if (items.length === 0) {
                        setError(`${platform.name} returned no official accounts. An account was not invented.`);
                      }
                    })}
                  >
                    {busy === "accounts" ? "Loading accounts…" : `Load official ${platform.name} accounts`}
                  </Button>
                  {accounts.length > 0 ? (
                    <>
                      <SelectField
                        label={`${platform.name} account`}
                        value={pickedAccount}
                        onChange={setPickedAccount}
                        options={accounts.map((item) => ({ value: item.id, label: item.label }))}
                      />
                      <Button
                        appearance="primary"
                        disabled={busy !== null || !pickedAccount}
                        onClick={() => void run("pick", () => api.selectConnectionAccount(businessId, connection.id, pickedAccount))}
                      >
                        {busy === "pick" ? "Saving…" : "Use this account"}
                      </Button>
                    </>
                  ) : null}
                </div>
              ) : null}

              <div>
                <p className="section-kicker">Adapter capabilities</p>
                <div className="caps">
                  {caps.map(([label, on]) => (
                    <span key={label} className={on ? "cap is-on" : "cap"}>{label}</span>
                  ))}
                </div>
              </div>

              <div className="conn-actions">
                {!isSignedIn(connection) ? (
                  <>
                    <Button
                      appearance="primary"
                      disabled={start.isPending || (!connection && connections.length >= maxConnections)}
                      onClick={() => start.mutate(platform.code)}
                    >
                      {start.isPending ? "Opening official login…" : platform.authMode === "Assisted" ? "Enable assisted" : `Sign in to ${platform.name}`}
                    </Button>
                    {connection ? (
                      <Button appearance="subtle" disabled={busy !== null} onClick={() => void run("disconnect", () => api.disconnectConnection(businessId, connection.id))}>
                        Discard
                      </Button>
                    ) : null}
                  </>
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
                      disabled={busy !== null || start.isPending}
                      onClick={() => start.mutate(platform.code)}
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

function OfficialAppsForm({
  open,
  apps,
  onToggle,
  onSaved
}: {
  open: boolean;
  apps: OfficialOAuthApps;
  onToggle: () => void;
  onSaved: () => Promise<unknown> | void;
}) {
  const [googleId, setGoogleId] = useState("");
  const [googleSecret, setGoogleSecret] = useState("");
  const [metaId, setMetaId] = useState("");
  const [metaSecret, setMetaSecret] = useState("");
  const [linkedId, setLinkedId] = useState("");
  const [linkedSecret, setLinkedSecret] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const google = apps.apps.find((app) => app.provider === "GOOGLE");
  const meta = apps.apps.find((app) => app.provider === "META");
  const linked = apps.apps.find((app) => app.provider === "LINKEDIN");

  return (
    <div className="oauth-apps">
      <Button appearance="subtle" onClick={onToggle}>
        {open ? "Hide official OAuth apps" : "Official OAuth apps"}
      </Button>
      {open ? (
        <form
          className="id-form oauth-apps-form"
          onSubmit={async (event) => {
            event.preventDefault();
            setBusy(true);
            setError(null);
            try {
              await api.saveOAuthApps({
                googleClientId: googleId || undefined,
                googleClientSecret: googleSecret || undefined,
                metaClientId: metaId || undefined,
                metaClientSecret: metaSecret || undefined,
                linkedInClientId: linkedId || undefined,
                linkedInClientSecret: linkedSecret || undefined
              });
              setGoogleSecret("");
              setMetaSecret("");
              setLinkedSecret("");
              await onSaved();
            } catch (err) {
              setError(err instanceof ApiError ? err.title : "Official app credentials were not saved.");
            } finally {
              setBusy(false);
            }
          }}
        >
          <p>
            Create apps at Google Cloud, Meta Developers, and LinkedIn Developers. Register this exact redirect URI:
            <code> {apps.redirectUri}</code>
          </p>
          <Field label="Google client id" value={googleId} onChange={setGoogleId} hint={google?.ready ? `Saved ${google.clientIdMasked}` : "Used for Google, YouTube, Search Console, Ads, Analytics"} />
          <Field label="Google client secret" type="password" value={googleSecret} onChange={setGoogleSecret} />
          <Field label="Meta client id" value={metaId} onChange={setMetaId} hint={meta?.ready ? `Saved ${meta.clientIdMasked}` : "Used for Facebook and Instagram"} />
          <Field label="Meta client secret" type="password" value={metaSecret} onChange={setMetaSecret} />
          <Field label="LinkedIn client id" value={linkedId} onChange={setLinkedId} hint={linked?.ready ? `Saved ${linked.clientIdMasked}` : undefined} />
          <Field label="LinkedIn client secret" type="password" value={linkedSecret} onChange={setLinkedSecret} />
          {error ? <p className="note-err" role="alert">{error}</p> : null}
          <div className="id-form-actions">
            <Button appearance="primary" disabled={busy}>
              {busy ? "Saving…" : "Save official apps"}
            </Button>
          </div>
        </form>
      ) : null}
    </div>
  );
}
