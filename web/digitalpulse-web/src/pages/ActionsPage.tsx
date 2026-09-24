import { Button } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type ActionWorkspace, type WorkAction } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";
import { Field } from "../design/Field";

export function ActionsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["actions", businessId],
    queryFn: () => api.actions(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening the action center" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before queueing actions." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading automation policy" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Action center unavailable" />;

  return <ActionWorkspaceView businessId={businessId} data={query.data} />;
}

function ActionWorkspaceView({ businessId, data }: { businessId: string; data: ActionWorkspace }) {
  const queryClient = useQueryClient();
  const [kind, setKind] = useState(data.kinds[0]?.code ?? "analyze-website");
  const [title, setTitle] = useState("");
  const [mode, setMode] = useState(data.policy.mode);
  const [allowLowRiskAuto, setAllowLowRiskAuto] = useState(data.policy.allowLowRiskAuto);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(data.actions[0]?.id ?? null);
  const selected = data.actions.find((item) => item.id === selectedId) ?? data.actions[0] ?? null;
  const selectedKind = data.kinds.find((item) => item.code === kind);

  async function refresh(nextId?: string) {
    await queryClient.invalidateQueries({ queryKey: ["actions"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    if (nextId) setSelectedId(nextId);
  }

  const savePolicy = useMutation({
    mutationFn: () => api.updateActionPolicy(businessId, { mode, allowLowRiskAuto, maxAttempts: data.policy.maxAttempts }),
    onSuccess: async () => {
      setError(null);
      setSuccess("Policy stored. Full Auto still cannot invent a live provider write.");
      await refresh();
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The automation policy could not be saved.");
    }
  });

  const enqueue = useMutation({
    mutationFn: () => api.enqueueAction(businessId, { kind, title, targetId: null, targetLabel: null }),
    onSuccess: async (action) => {
      setError(null);
      setSuccess(action.holdReason);
      setTitle("");
      await refresh(action.id);
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The action could not be queued.");
    }
  });

  async function run(action: () => Promise<WorkAction>, ok: string) {
    setError(null);
    try {
      const result = await action();
      setSuccess(result.holdReason || ok);
      await refresh(result.id);
    } catch (err) {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The action engine could not continue.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Actions + Autopilot</p>
        <h1 className="display display-page command-title">Action center</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          {data.policy.mode} · {data.actionsUsedThisMonth} of {data.actionsPerMonth} actions this month
        </p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Automation policy</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              savePolicy.mutate();
            }}
          >
            <label className="dp-field">
              <span>Mode</span>
              <select value={mode} onChange={(event) => setMode(event.target.value)}>
                <option value="Assisted">Assisted</option>
                <option value="FullAuto">Full Auto</option>
              </select>
            </label>
            <label className="dp-field">
              <span>Low-risk autopilot</span>
              <select value={allowLowRiskAuto ? "yes" : "no"} onChange={(event) => setAllowLowRiskAuto(event.target.value === "yes")}>
                <option value="yes">Allowed for low-risk internal work</option>
                <option value="no">Always ask</option>
              </select>
            </label>
            <p className="ink-muted">High-risk and external writes always need approval. Max retries: {data.policy.maxAttempts}.</p>
            <Button appearance="primary" type="submit" disabled={savePolicy.isPending}>
              {savePolicy.isPending ? "Saving…" : "Save policy"}
            </Button>
          </form>
        </article>
        <article className="panel">
          <h2>Queue an action</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              enqueue.mutate();
            }}
          >
            <label className="dp-field">
              <span>Kind</span>
              <select value={kind} onChange={(event) => setKind(event.target.value)}>
                {data.kinds.map((item) => (
                  <option key={item.code} value={item.code}>{item.name}</option>
                ))}
              </select>
            </label>
            <p className="ink-muted">{selectedKind?.purpose}</p>
            <Field label="Title" value={title} onChange={setTitle} required />
            <Button appearance="primary" type="submit" disabled={enqueue.isPending || title.trim().length < 3}>
              {enqueue.isPending ? "Queueing…" : "Enqueue"}
            </Button>
          </form>
        </article>
      </div>

      <DataGrid
        noun="action"
        empty="Queue a low-risk check or a high-risk write. Live publishes stay held without an official adapter."
        columns={["Action", "Status", "Risk", "Autopilot"]}
        selectedId={selected?.id}
        rows={data.actions.map((item) => ({
          id: item.id,
          search: `${item.kind} ${item.title} ${item.status}`.toLowerCase(),
          cells: [item.title, item.status, item.risk, item.autopilotEligible ? "Eligible" : "Assisted"],
          actions: <button type="button" className="grid-action" onClick={() => setSelectedId(item.id)}>Open</button>
        }))}
        onRow={(id) => setSelectedId(id)}
      />

      {selected ? (
        <article className="panel">
          <p className="hero-kicker">{selected.kind}</p>
          <h2>{selected.title}</h2>
          <p className="ink-muted">{selected.holdReason}</p>
          <div className="row-line"><span>Status</span><span className={`sev ${selected.status === "Verified" ? "sev-ok" : selected.status === "Failed" || selected.status === "Escalated" ? "sev-warn" : "sev-hold"}`}>{selected.status}</span></div>
          <div className="row-line"><span>Idempotency</span><span>{selected.idempotencyKey}</span></div>
          <div className="row-line"><span>Attempts</span><span>{selected.attemptCount}</span></div>
          <div className="id-form-actions">
            {selected.status === "PendingApproval" ? (
              <Button appearance="primary" onClick={() => void run(() => api.approveAction(businessId, selected.id), "Approved.")}>Approve</Button>
            ) : null}
            {selected.status === "Approved" || selected.status === "Queued" ? (
              <Button appearance="primary" onClick={() => void run(() => api.executeAction(businessId, selected.id), "Executed.")}>Execute</Button>
            ) : null}
            {selected.status === "Failed" ? (
              <Button appearance="subtle" onClick={() => void run(() => api.retryAction(businessId, selected.id), "Retry recorded.")}>Retry</Button>
            ) : null}
            {selected.status === "Executed" || selected.status === "Assisted" ? (
              <Button appearance="subtle" onClick={() => void run(() => api.verifyAction(businessId, selected.id), "Verification recorded.")}>Verify</Button>
            ) : null}
          </div>
          {selected.attempts.map((attempt) => (
            <div className="row-line" key={`${attempt.ordinal}-${attempt.atUtc}`}>
              <span>Attempt {attempt.ordinal} · {attempt.outcome}</span>
              <span>{attempt.detail}</span>
            </div>
          ))}
          {selected.verification ? (
            <div className="row-line">
              <span>Verification</span>
              <span>{selected.verification.status}: {selected.verification.detail}</span>
            </div>
          ) : null}
        </article>
      ) : (
        <PageState mode="empty" title="No action selected" detail="Enqueue a check to see approval, attempts, and verification." />
      )}
    </section>
  );
}
