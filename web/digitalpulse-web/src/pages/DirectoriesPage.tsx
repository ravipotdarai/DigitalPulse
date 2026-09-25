import { Textarea } from "@fluentui/react-components";
import { Button } from "../design/Button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiError, api, type DirectoryTask, type DirectoryWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { ChannelCard } from "../design/ChannelCard";

export function DirectoriesPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["directories", businessId],
    queryFn: () => api.directories(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening directories" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before preparing IndiaMART or Justdial." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading directory capabilities" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Directories unavailable" />;

  return <DirectoryWorkspaceView businessId={businessId} data={query.data} />;
}

function DirectoryWorkspaceView({ businessId, data }: { businessId: string; data: DirectoryWorkspace }) {
  const [params] = useSearchParams();
  const focus = params.get("platform")?.toUpperCase();
  const providers = focus
    ? data.providers.filter((provider) => provider.capabilities.platformCode === focus)
    : data.providers;
  const shown = providers.length > 0 ? providers : data.providers;
  const tasks = focus
    ? data.tasks.filter((task) => task.platformCode === focus)
    : data.tasks;
  const heading = focus === "INDIAMART" ? "IndiaMART" : focus === "JUSTDIAL" ? "Justdial" : "Directories";
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [note, setNote] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(tasks[0]?.id ?? null);
  const selected = tasks.find((item) => item.id === selectedId) ?? tasks[0] ?? null;

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["directories"] });
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
      setError(err instanceof ApiError ? err.title : "The directory action failed.");
    }
  }

  const prepare = useMutation({
    mutationFn: (platformCode: string) => api.prepareDirectory(businessId, platformCode),
    onSuccess: async (task) => {
      setSelectedId(task.id);
      setSuccess("Assisted playbook prepared from the identity record. No unofficial write was sent.");
      await refresh();
    },
    onError: (err) => setError(err instanceof ApiError ? err.title : "Could not prepare the playbook.")
  });

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Growth</p>
        <h1 className="page-title">{heading}</h1>
        <p className="page-lead">{data.note}</p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="channel-grid">
        {shown.map((provider) => (
          <ChannelCard
            key={provider.capabilities.platformCode}
            code={provider.capabilities.platformCode}
            name={provider.capabilities.platformName}
            category={provider.capabilities.assistedOnly ? "Assisted directory" : "API directory"}
            rows={[
              { label: "Connection", value: provider.connectionStatus ?? "Not enabled", tone: provider.connectionStatus === "Connected" ? "ok" : "hold" },
              { label: "Official read", value: provider.readStatus, tone: "hold" },
              { label: "Official write", value: "Assisted", tone: "hold" }
            ]}
            footer={
              <div className="id-form-actions">
                <Button appearance="primary" disabled={prepare.isPending} onClick={() => prepare.mutate(provider.capabilities.platformCode)}>
                  Prepare playbook
                </Button>
                <Button appearance="subtle" onClick={() => void run(() => api.monitorDirectory(businessId, provider.capabilities.platformCode), "Monitor stored. Live listing pages were not scraped.")}>
                  Monitor
                </Button>
              </div>
            }
          >
            <p className="ink-muted">{provider.readDetail}</p>
          </ChannelCard>
        ))}
      </div>

      {tasks.length === 0 ? (
        <div className="dp-empty dp-surface dp-empty-lg">
          <strong>No assisted tasks</strong>
          <p>Enable {heading === "Directories" ? "IndiaMART or Justdial" : heading} in Social login or Connection Center, then prepare a playbook from the business profile.</p>
        </div>
      ) : (
        <div className="workspace-split">
          <aside className="panel">
            <h2>Tasks</h2>
            {tasks.map((task) => (
              <button
                key={task.id}
                type="button"
                className={task.id === selected?.id ? "row-line row-button is-on" : "row-line row-button"}
                aria-pressed={task.id === selected?.id}
                onClick={() => setSelectedId(task.id)}
              >
                <span>
                  <strong>{task.platformCode}</strong>
                  <p className="ink-muted flush">{task.status}</p>
                </span>
                <span className={`sev ${task.status === "Verified" ? "sev-ok" : "sev-warn"}`}>{task.steps.filter((s) => s.completedAtUtc).length}/{task.steps.length}</span>
              </button>
            ))}
          </aside>
          {selected ? <TaskDetail businessId={businessId} task={selected} note={note} setNote={setNote} run={run} /> : null}
        </div>
      )}
    </section>
  );
}

function TaskDetail({
  businessId,
  task,
  note,
  setNote,
  run
}: {
  businessId: string;
  task: DirectoryTask;
  note: string;
  setNote: (value: string) => void;
  run: (action: () => Promise<unknown>, ok: string) => Promise<void>;
}) {
  return (
    <article className="panel">
      <p className="hero-kicker">{task.kind} · {task.status}</p>
      <h2>{task.preparedName}</h2>
      <div className="row-line"><span>Phone</span><span>{task.preparedPhone ?? "—"}</span></div>
      <div className="row-line"><span>Website</span><span>{task.preparedWebsite ?? "—"}</span></div>
      <div className="row-line"><span>Category</span><span>{task.preparedCategory ?? "—"}</span></div>
      <div className="row-line"><span>Services</span><span>{task.preparedServices ?? "—"}</span></div>
      {task.monitorDetail ? <p className="ink-muted">{task.monitorDetail}</p> : null}
      <h3 className="spaced">Assisted steps</h3>
      {task.steps.map((step) => (
        <div className="row-line" key={step.id}>
          <div>
            <strong>{step.ordinal}. {step.title}</strong>
            <p className="ink-muted meta-line">{step.detail}</p>
          </div>
          {step.completedAtUtc ? (
            <span className="sev sev-ok">Done</span>
          ) : (
            <Button appearance="subtle" onClick={() => void run(() => api.completeDirectoryStep(businessId, task.id, step.id), "Step marked complete. The directory was not written by DigitalPulse.")}>
              Mark done
            </Button>
          )}
        </div>
      ))}
      {task.status === "AwaitingVerification" ? (
        <>
          <Textarea value={note} onChange={(_, next) => setNote(next.value)} placeholder="What you confirmed on the official listing" aria-label="Verification note" resize="vertical" />
          <Button appearance="primary" onClick={() => void run(() => api.verifyDirectory(businessId, task.id, note), "Assisted update verified. DigitalPulse did not scrape the live listing.")}>
            Verify
          </Button>
        </>
      ) : null}
      {task.verificationNote ? <p>Verified: {task.verificationNote}</p> : null}
    </article>
  );
}
