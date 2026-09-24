import { Button } from "@fluentui/react-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api, type AgencyWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid, GridAction } from "../design/DataGrid";

export function AgencyPage() {
  const query = useQuery({ queryKey: ["agency"], queryFn: api.agency });
  if (query.isLoading) return <PageState mode="loading" title="Opening agency workspace" />;
  if (query.isError || !query.data) {
    const detail = query.error instanceof ApiError ? query.error.title : "Agency workspace is available on Agency tenants.";
    return <PageState mode="error" title="Agency workspace unavailable" detail={detail} />;
  }
  return <AgencyWorkspaceView data={query.data} />;
}

function AgencyWorkspaceView({ data }: { data: AgencyWorkspace }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [website, setWebsite] = useState("");
  const [contactName, setContactName] = useState("");
  const [contactEmail, setContactEmail] = useState("");
  const [displayName, setDisplayName] = useState(data.whiteLabel.displayName);
  const [supportEmail, setSupportEmail] = useState(data.whiteLabel.supportEmail ?? "");
  const [primaryColor, setPrimaryColor] = useState(data.whiteLabel.primaryColor);
  const [customDomain, setCustomDomain] = useState(data.whiteLabel.customDomain ?? "");
  const [decision, setDecision] = useState("");
  const firstClient = data.clients[0];

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["agency"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  }

  async function run(action: () => Promise<unknown>, ok: string) {
    setError(null);
    try {
      await action();
      setSuccess(ok);
      await refresh();
    } catch (err) {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "Agency workspace could not continue.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Agency + White Label</p>
        <h1 className="display display-page command-title">{data.tenantName}</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          {data.planName} · {data.clients.length} of {data.clientCap} client businesses
          {" · "}
          {data.whiteLabelEntitled ? "White-label entitled" : "White-label not on this plan"}
        </p>
        <p className="ink-muted">{data.whiteLabel.holdReason}</p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Add a client business</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              run(
                () => api.createAgencyClient({
                  name,
                  website: website || null,
                  status: "Prospect",
                  contactName: contactName || null,
                  contactEmail: contactEmail || null
                }),
                "Client business stored on this Agency tenant."
              );
              setName("");
              setWebsite("");
            }}
          >
            <label className="dp-field">
              <span>Client name</span>
              <input value={name} onChange={(event) => setName(event.target.value)} required maxLength={160} />
            </label>
            <label className="dp-field">
              <span>Website</span>
              <input value={website} onChange={(event) => setWebsite(event.target.value)} maxLength={2048} />
            </label>
            <label className="dp-field">
              <span>Contact</span>
              <input value={contactName} onChange={(event) => setContactName(event.target.value)} maxLength={160} />
            </label>
            <label className="dp-field">
              <span>Contact email</span>
              <input value={contactEmail} onChange={(event) => setContactEmail(event.target.value)} maxLength={256} />
            </label>
            <div className="id-form-actions spaced">
              <Button appearance="primary" type="submit">Add client</Button>
            </div>
          </form>
        </article>
        <article className="panel">
          <h2>White-label branding</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              run(
                () => api.updateWhiteLabel({
                  displayName,
                  supportEmail: supportEmail || null,
                  primaryColor,
                  customDomain: customDomain || null,
                  enabled: true
                }),
                "Branding stored. Custom-domain hosting stays held."
              );
            }}
          >
            <label className="dp-field">
              <span>Display name</span>
              <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} required maxLength={160} />
            </label>
            <label className="dp-field">
              <span>Support email</span>
              <input value={supportEmail} onChange={(event) => setSupportEmail(event.target.value)} maxLength={256} />
            </label>
            <label className="dp-field">
              <span>Primary color</span>
              <input value={primaryColor} onChange={(event) => setPrimaryColor(event.target.value)} maxLength={7} />
            </label>
            <label className="dp-field">
              <span>Custom domain</span>
              <input value={customDomain} onChange={(event) => setCustomDomain(event.target.value)} maxLength={253} placeholder="reports.agency.example" />
            </label>
            <div className="id-form-actions spaced">
              <Button appearance="primary" type="submit">Store branding</Button>
            </div>
          </form>
        </article>
      </div>

      <article className="panel">
        <h2>Client businesses</h2>
        <DataGrid
          noun="client"
          empty="Add a client business. Agency clients are extra businesses on this tenant, not child tenants."
          columns={["Client", "Status", "Findings", "Last check"]}
          rows={data.clients.map((item) => ({
            id: item.id,
            search: `${item.name} ${item.status}`.toLowerCase(),
            cells: [
              item.website ? `${item.name} · ${item.website}` : item.name,
              item.status,
              String(item.openFindingCount),
              item.lastScanAtUtc ? new Date(item.lastScanAtUtc).toLocaleDateString() : "Never"
            ]
          }))}
        />
        {firstClient ? (
          <p className="ink-muted">
            Open the first client identity record as a <Link className="text-link" to={`/app/businesses/${firstClient.businessId}`}>business</Link>.
          </p>
        ) : null}
      </article>

      <div className="band band-2">
        <article className="panel">
          <h2>Workflows</h2>
          <p className="ink-muted">Onboarding, monthly review, presence audit, and white-label review. Presence audits hold without a stored check. Custom-domain review stays held.</p>
          <div className="id-form-actions spaced">
            <Button appearance="secondary" onClick={() => run(() => api.startAgencyWorkflow({ kind: "ClientOnboarding", clientId: firstClient?.id ?? null }), "Client onboarding started.")}>
              Start onboarding
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.startAgencyWorkflow({ kind: "PresenceAudit", clientId: firstClient?.id ?? null }), "Presence audit started.")}>
              Start presence audit
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.startAgencyWorkflow({ kind: "WhiteLabelReview" }), "White-label review started.")}>
              Start white-label review
            </Button>
          </div>
          <DataGrid
            noun="workflow"
            empty="Start a workflow for a client or for white-label review."
            columns={["Kind", "Status", "Step", "Hold"]}
            rows={data.workflows.map((item) => ({
              id: item.id,
              search: `${item.kind} ${item.status}`.toLowerCase(),
              cells: [item.kind, item.status, item.currentStepName, item.holdReason || "—"],
              actions: item.status === "Completed" ? null : (
                <GridAction onClick={() => run(() => api.advanceAgencyWorkflow(item.id, { note: "Advanced from the agency workspace." }), "Workflow advanced from stored evidence.")}>
                  Advance
                </GridAction>
              )
            }))}
          />
        </article>
        <article className="panel">
          <h2>Agency reports</h2>
          <p className="ink-muted">Observed Fact, Recommendation, AI Interpretation, and Customer Decision stay separate. AI commentary is not invented.</p>
          <div className="id-form-actions spaced">
            <Button appearance="primary" onClick={() => run(() => api.assembleAgencyReport({ scope: "Portfolio" }), "Portfolio report assembled from stored work.")}>
              Assemble portfolio
            </Button>
            <Button
              appearance="secondary"
              disabled={!firstClient}
              onClick={() => run(() => api.assembleAgencyReport({ scope: "Client", clientId: firstClient?.id }), "Client report assembled from that business only.")}
            >
              Assemble first client
            </Button>
          </div>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              const report = data.reports[0];
              if (!report) return;
              run(() => api.recordAgencyDecision(report.id, { decision }), "Customer decision recorded.");
              setDecision("");
            }}
          >
            <label className="dp-field">
              <span>Decision on the latest report</span>
              <input value={decision} onChange={(event) => setDecision(event.target.value)} maxLength={500} />
            </label>
            <Button appearance="secondary" type="submit" disabled={!data.reports[0]}>Record decision</Button>
          </form>
        </article>
      </div>

      <article className="panel">
        <h2>Stored reports</h2>
        <DataGrid
          noun="report"
          empty="Assemble a portfolio or client report from stored checks and alerts."
          columns={["Title", "Scope", "Observed fact", "AI interpretation"]}
          rows={data.reports.map((item) => ({
            id: item.id,
            search: `${item.title} ${item.scope}`.toLowerCase(),
            cells: [item.title, item.scope, item.observedFact, item.aiInterpretation]
          }))}
        />
      </article>
    </section>
  );
}
