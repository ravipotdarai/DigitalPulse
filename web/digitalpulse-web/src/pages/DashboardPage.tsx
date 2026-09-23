import { Button } from "@fluentui/react-components";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { PageState } from "../components/PageState";
import { HealthRing } from "../design/HealthRing";
import { PresenceTimeline } from "../design/PresenceLoop";
import { DataGrid } from "../design/DataGrid";


export function DashboardPage() {
  const navigate = useNavigate();
  const query = useQuery({ queryKey: ["dashboard"], queryFn: api.dashboard });
  const firstId = query.data?.businesses[0]?.id;
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

  if (query.isLoading) return <PageState mode="loading" title="Opening command center" />;
  if (query.isError) return <PageState mode="error" title="Command center unavailable" detail="Finish onboarding if you have not selected a plan." />;
  const data = query.data!;
  const coverage = identity.data ? scoreIdentity(identity.data) : 0;

  return (
    <section className="command">
      <div className="command-hero">
        <HealthRing value={coverage} label="Digital presence health from the identity record" />
        <div>
          <p className="hero-kicker">{data.tenantType} · {data.planName}</p>
          <h1 className="display" style={{ fontSize: "clamp(2rem, 5vw, 3.6rem)", margin: 0 }}>{data.tenantName}</h1>
          <p style={{ color: "var(--muted)", margin: "0.55rem 0 0.9rem" }}>
            Health starts from the identity record. Open findings come from the latest DigitalPulse Check, not invented platform listings.
          </p>
          <PresenceTimeline now={data.lastScanAtUtc ? "findings" : "identity"} />
        </div>
      </div>

      <div className="band band-2">
        <article className="panel">
          <h2>Critical findings</h2>
          {data.topFindings.length === 0 ? (
            <div className="dp-empty" style={{ minHeight: "8rem" }}>
              <strong>{data.lastScanAtUtc ? "None open" : "No scan has run"}</strong>
              <p>
                {data.lastScanAtUtc
                  ? "The latest check has no open high-severity findings."
                  : "Run DigitalPulse Check to compare identity, website, and authorized platforms."}
              </p>
            </div>
          ) : (
            data.topFindings.map((finding) => (
              <div className="row-line" key={finding.id}>
                <span>{finding.title}</span>
                <span className={`sev ${finding.severity === "High" || finding.severity === "Critical" ? "sev-crit" : "sev-warn"}`}>
                  {finding.severity}
                </span>
              </div>
            ))
          )}
          <Button appearance="subtle" onClick={() => navigate("/app/findings")}>Open findings</Button>
        </article>
        <article className="panel">
          <h2>AI recommendations</h2>
          {identity.data && !identity.data.business.website ? (
            <div className="row-line"><span>Add the official website</span><span className="sev sev-warn">Identity</span></div>
          ) : null}
          {identity.data && identity.data.contacts.length === 0 ? (
            <div className="row-line"><span>Record a phone or email</span><span className="sev sev-warn">Identity</span></div>
          ) : null}
          {identity.data && !identity.data.facts.some((fact) => fact.status === "Approved") ? (
            <div className="row-line"><span>Approve one publishable fact</span><span className="sev sev-hold">Fact</span></div>
          ) : null}
          {identity.data && identity.data.business.website && identity.data.contacts.length > 0 && identity.data.facts.some((f) => f.status === "Approved") ? (
            <p style={{ color: "var(--muted)" }}>No identity recommendations. Run DigitalPulse Check for evidence-backed findings.</p>
          ) : null}
          <Button appearance="subtle" onClick={() => firstId && navigate(`/app/businesses/${firstId}`)}>Open identity</Button>
        </article>
      </div>

      <div className="band band-3">
        <article className="panel">
          <h2>Active actions</h2>
          {data.socialDraftCount === 0 && data.socialBlockedCount === 0 ? (
            <div className="dp-empty" style={{ minHeight: "7rem" }}>
              <strong>Queue empty</strong>
              <p>Social drafts wait in Social. Live Google or Meta posts are not sent.</p>
            </div>
          ) : (
            <>
              <div className="row-line"><span>Social drafts</span><span className="sev sev-hold">{data.socialDraftCount}</span></div>
              <div className="row-line"><span>Publish holds</span><span className={`sev ${data.socialBlockedCount ? "sev-warn" : "sev-ok"}`}>{data.socialBlockedCount}</span></div>
              <Button appearance="subtle" onClick={() => navigate("/app/social")}>Open social</Button>
            </>
          )}
        </article>
        <article className="panel">
          <h2>Platform health</h2>
          {(connections.data?.catalog ?? []).slice(0, 6).map((platform) => {
            const link = connections.data?.connections.find((item) => item.platformCode === platform.code);
            return (
              <div className="row-line" key={platform.code}>
                <span>{platform.name}</span>
                <span className={`sev ${link?.status === "Connected" ? "sev-ok" : "sev-hold"}`}>{link?.lastHealthStatus ?? link?.status ?? "Not connected"}</span>
              </div>
            );
          })}
        </article>
        <article className="panel">
          <h2>Monitoring</h2>
          <div className="row-line"><span>Signals</span><span className={`sev ${data.lastScanAtUtc ? "sev-ok" : "sev-hold"}`}>{data.lastScanAtUtc ? "Last check stored" : "Idle"}</span></div>
          <div className="row-line"><span>Open findings</span><span className={`sev ${data.highFindingCount ? "sev-crit" : data.openFindingCount ? "sev-warn" : "sev-hold"}`}>{data.openFindingCount}</span></div>
          <div className="row-line"><span>Website</span><span className={`sev ${data.lastWebsiteAtUtc ? "sev-ok" : "sev-hold"}`}>{data.lastWebsiteAtUtc ? `${data.websiteObservationCount} observations` : "Not analyzed"}</span></div>
          <div className="row-line"><span>Search</span><span className="sev sev-hold">{data.searchProvider}</span></div>
        </article>
      </div>

      <article>
        <h2 className="display" style={{ fontSize: "1.4rem", margin: "0.4rem 0 0.7rem" }}>Businesses</h2>
        {data.businesses.length === 0 ? (
          <PageState mode="empty" title="No businesses yet" />
        ) : (
          <DataGrid
            noun="business"
            empty="Create a business during onboarding."
            columns={["Business", "Website", "Locations"]}
            rows={data.businesses.map((business) => ({
              id: business.id,
              search: `${business.name} ${business.website ?? ""}`.toLowerCase(),
              cells: [business.name, business.website ?? "—", String(business.locationCount)],
              actions: <button type="button" className="grid-action" onClick={() => navigate(`/app/businesses/${business.id}`)}>Identity</button>
            }))}
            onRow={(id) => navigate(`/app/businesses/${id}`)}
          />
        )}
      </article>
    </section>
  );
}

function scoreIdentity(data: Awaited<ReturnType<typeof api.identity>>) {
  const checks = [
    Boolean(data.business.name.trim()),
    Boolean(data.business.website),
    Boolean(data.business.industryCode),
    Boolean(data.business.foundedYear),
    data.contacts.length > 0,
    data.categories.length > 0,
    data.services.length > 0,
    data.facts.some((fact) => fact.status === "Approved")
  ];
  return Math.round((checks.filter(Boolean).length / checks.length) * 100);
}
