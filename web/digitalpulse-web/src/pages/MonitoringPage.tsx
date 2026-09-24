import { Button } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type MonitoringWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";
import { Field } from "../design/Field";

export function MonitoringPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["monitoring", businessId],
    queryFn: () => api.monitoring(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening monitoring" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before scheduling monitoring." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading monitoring workspace" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Monitoring workspace unavailable" />;

  return <MonitoringWorkspaceView businessId={businessId} data={query.data} />;
}

function MonitoringWorkspaceView({ businessId, data }: { businessId: string; data: MonitoringWorkspace }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [website, setWebsite] = useState("");
  const [decision, setDecision] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedRunId, setSelectedRunId] = useState<string | null>(data.runs[0]?.id ?? null);
  const [selectedReportId, setSelectedReportId] = useState<string | null>(data.reports[0]?.id ?? null);
  const selectedRun = data.runs.find((item) => item.id === selectedRunId) ?? data.runs[0] ?? null;
  const selectedReport = data.reports.find((item) => item.id === selectedReportId) ?? data.reports[0] ?? null;

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["monitoring"] });
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
      setError(err instanceof ApiError ? err.title : "Monitoring could not continue.");
    }
  }

  const addCompetitor = useMutation({
    mutationFn: () => api.addCompetitor(businessId, { name, website: website || null, notes: null }),
    onSuccess: async () => {
      setError(null);
      setSuccess("Competitor recorded. Official listing reads stay held.");
      setName("");
      setWebsite("");
      await refresh();
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The competitor could not be added.");
    }
  });

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Monitoring + Reporting</p>
        <h1 className="display display-page command-title">Presence watch</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          Every {data.schedule.intervalHours} hour(s) · {data.schedule.due ? "Due now" : "Waiting for the next interval"} · {data.schedule.holdReason}
        </p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Run and report</h2>
          <p className="ink-muted">Manual runs are allowed immediately. Scheduled ticks respect the plan interval. Reports keep Observed Fact, Recommendation, AI Interpretation, and Customer Decision separate.</p>
          <div className="id-form-actions spaced">
            <Button appearance="primary" onClick={() => run(() => api.runMonitoring(businessId), "Monitoring recorded stored health. Live metrics were not invented.")}>
              Run monitoring
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.assembleReport(businessId), "Report assembled from stored observations.")}>
              Assemble report
            </Button>
          </div>
        </article>

        <article className="panel">
          <h2>Add a competitor</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              if (!name.trim()) {
                setError("Enter the competitor name.");
                return;
              }
              addCompetitor.mutate();
            }}
          >
            <Field label="Name" value={name} onChange={setName} required />
            <Field label="Website" value={website} onChange={setWebsite} />
            <div className="id-form-actions">
              <Button appearance="primary" type="submit" disabled={addCompetitor.isPending}>Record competitor</Button>
            </div>
          </form>
        </article>
      </div>

      <article className="panel">
        <h2>Kinds</h2>
        <DataGrid
          noun="check"
          empty="Monitoring kinds load from the catalog."
          columns={["Check", "Live API", "Purpose"]}
          rows={data.kinds.map((item) => ({
            id: item.code,
            search: `${item.name} ${item.purpose}`.toLowerCase(),
            cells: [item.name, item.canObserveWithoutLiveApi ? "Stored evidence" : "Held without live API", item.purpose]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Runs</h2>
        <DataGrid
          noun="run"
          empty="Run monitoring to store the first observations."
          columns={["Trigger", "Status", "Summary"]}
          rows={data.runs.map((item) => ({
            id: item.id,
            search: `${item.trigger} ${item.status} ${item.summary}`.toLowerCase(),
            cells: [item.trigger, item.status, item.summary],
            actions: <button type="button" className="grid-action" onClick={() => setSelectedRunId(item.id)}>Open</button>
          }))}
          onRow={(id) => setSelectedRunId(id)}
        />
        {selectedRun ? (
          <div className="id-form" style={{ marginTop: "1rem" }}>
            <p className="ink-muted">{selectedRun.summary}</p>
            <DataGrid
              noun="observation"
              empty="This run has no stored results."
              columns={["Kind", "Status", "Observed fact", "Recommendation"]}
              rows={selectedRun.results.map((item) => ({
                id: item.id,
                search: `${item.kind} ${item.status} ${item.observedFact}`.toLowerCase(),
                cells: [item.title, item.status, item.observedFact, item.recommendation]
              }))}
            />
          </div>
        ) : null}
      </article>

      <article className="panel">
        <h2>Alerts</h2>
        <DataGrid
          noun="alert"
          empty="No alerts. Changed observations and unhealthy websites raise alerts."
          columns={["Severity", "Status", "Title", "Detail"]}
          rows={data.alerts.map((item) => ({
            id: item.id,
            search: `${item.severity} ${item.status} ${item.title}`.toLowerCase(),
            cells: [item.severity, item.status, item.title, item.detail],
            actions: item.status === "Open"
              ? (
                <button
                  type="button"
                  className="grid-action"
                  onClick={() => run(() => api.acknowledgeAlert(businessId, item.id), "Alert acknowledged.")}
                >
                  Acknowledge
                </button>
              )
              : undefined
          }))}
        />
      </article>

      <article className="panel">
        <h2>Competitors</h2>
        <DataGrid
          noun="competitor"
          empty="Record a competitor by name. Official pages are not invented."
          columns={["Name", "Website", "Notes"]}
          rows={data.competitors.map((item) => ({
            id: item.id,
            search: `${item.name} ${item.website ?? ""}`.toLowerCase(),
            cells: [item.name, item.website ?? "—", item.notes ?? "—"]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Reports</h2>
        <DataGrid
          noun="report"
          empty="Assemble a pulse from the latest stored run."
          columns={["Kind", "Title", "Decision"]}
          rows={data.reports.map((item) => ({
            id: item.id,
            search: `${item.kind} ${item.title} ${item.customerDecision ?? ""}`.toLowerCase(),
            cells: [item.kind, item.title, item.customerDecision ?? "Waiting"],
            actions: <button type="button" className="grid-action" onClick={() => setSelectedReportId(item.id)}>Open</button>
          }))}
          onRow={(id) => setSelectedReportId(id)}
        />
        {selectedReport ? (
          <div className="id-form" style={{ marginTop: "1rem" }}>
            <p><strong>Observed fact.</strong> {selectedReport.observedFact}</p>
            <p><strong>Recommendation.</strong> {selectedReport.recommendation}</p>
            <p><strong>AI interpretation.</strong> {selectedReport.aiInterpretation}</p>
            <p><strong>Customer decision.</strong> {selectedReport.customerDecision ?? "Not recorded."}</p>
            <p className="ink-muted">{selectedReport.holdReason}</p>
            <Field label="Record a decision" value={decision} onChange={setDecision} />
            <div className="id-form-actions">
              <Button
                appearance="primary"
                disabled={!decision.trim()}
                onClick={() => run(() => api.recordReportDecision(businessId, selectedReport.id, { decision }), "Customer decision stored.")}
              >
                Save decision
              </Button>
            </div>
          </div>
        ) : null}
      </article>
    </section>
  );
}
