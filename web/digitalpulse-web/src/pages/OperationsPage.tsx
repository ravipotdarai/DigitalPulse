import { Button } from "../design/Button";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type OperationsWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";

export function OperationsPage() {
  const query = useQuery({ queryKey: ["operations"], queryFn: api.operations });
  if (query.isLoading) return <PageState mode="loading" title="Opening operations" />;
  if (query.isError || !query.data) {
    const detail = query.error instanceof ApiError ? query.error.title : "Finish plan selection before opening operations.";
    return <PageState mode="error" title="Operations workspace unavailable" detail={detail} />;
  }
  return <OperationsWorkspaceView data={query.data} />;
}

function OperationsWorkspaceView({ data }: { data: OperationsWorkspace }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const latest = data.backups[0];

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["operations"] });
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
      setError(err instanceof ApiError ? err.title : "Operations could not continue.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Production hardening</p>
        <h1 className="display display-page command-title">Operations</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          {data.environmentName} · {data.hostRole}
          {" · "}
          {data.rateLimitingEnabled ? `${data.rateLimitPerMinute}/min` : "rate limit off in tests"}
          {" · "}
          {data.keyVaultConfigured ? "Key Vault" : "Key Vault held"}
          {" · "}
          {data.appInsightsConfigured ? "Insights" : "Insights held"}
        </p>
        {data.readiness ? <p className="ink-muted">{data.readiness.status} · {data.readiness.holdReason}</p> : null}
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Backup and restore</h2>
          <p className="ink-muted">Snapshots store row counts for this tenant only. Azure Backup is not invented. Restore is a same-tenant checksum dry-run.</p>
          <div className="id-form-actions spaced">
            <Button appearance="primary" onClick={() => run(() => api.captureBackup(), "Logical snapshot stored. Azure Backup stays held.")}>
              Capture snapshot
            </Button>
            <Button
              appearance="secondary"
              disabled={!latest}
              onClick={() => run(() => api.restoreBackup(latest!.id), "Same-tenant restore verified. A live Azure restore was not invented.")}
            >
              Verify latest restore
            </Button>
          </div>
        </article>
        <article className="panel">
          <h2>Drills and inventory</h2>
          <p className="ink-muted">Failover and scale stay held on a single host. Package lists come from project files. CVEs are not invented.</p>
          <div className="id-form-actions spaced">
            <Button appearance="secondary" onClick={() => run(() => api.startDrill({ kind: "Failover" }), "Failover drill recorded as held.")}>
              Failover drill
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.startDrill({ kind: "Scaling" }), "Scaling drill recorded as held.")}>
              Scaling drill
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.runInventory({ kind: "Dependency" }), "Dependency inventory recorded.")}>
              Inventory packages
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.runInventory({ kind: "Container" }), "Container inventory recorded.")}>
              Inventory images
            </Button>
            <Button appearance="primary" onClick={() => run(() => api.assembleReadiness(), "Readiness assembled from stored gates.")}>
              Assemble readiness
            </Button>
          </div>
        </article>
      </div>

      <article className="panel">
        <h2>Cost controls</h2>
        <DataGrid
          noun="meter"
          empty="Select a plan to see catalog budgets."
          columns={["Meter", "Used", "Included", "Note"]}
          rows={data.cost.map((item) => ({
            id: item.meter,
            search: item.meter.toLowerCase(),
            cells: [item.meter, String(item.used), String(item.included), item.note]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Readiness gates</h2>
        <DataGrid
          noun="gate"
          empty="Assemble readiness to record stored security, backup, and telemetry gates."
          columns={["Gate", "Outcome", "Detail"]}
          rows={(data.readiness?.checks ?? []).map((item) => ({
            id: item.code,
            search: `${item.title} ${item.outcome}`.toLowerCase(),
            cells: [item.title, item.outcome, item.detail]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Snapshots</h2>
        <DataGrid
          noun="snapshot"
          empty="Capture a logical snapshot. Secrets never go into the manifest."
          columns={["Status", "Manifest", "Hold"]}
          rows={data.backups.map((item) => ({
            id: item.id,
            search: item.manifest.toLowerCase(),
            cells: [item.status, item.manifest, item.holdReason]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Audit</h2>
        <DataGrid
          noun="event"
          empty="Backup, restore, drills, and readiness write immutable tenant audits."
          columns={["Action", "Detail"]}
          rows={data.audits.map((item) => ({
            id: item.id,
            search: `${item.action} ${item.detail}`.toLowerCase(),
            cells: [item.action, item.detail]
          }))}
        />
      </article>
    </section>
  );
}
