import { Button, Input, Textarea } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type SocialWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";

export function SocialPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["social", businessId],
    queryFn: () => api.social(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening social workspace" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before drafting social content." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading social channels" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Social workspace unavailable" />;

  return <SocialWorkspaceView businessId={businessId} data={query.data} />;
}

function SocialWorkspaceView({ businessId, data }: { businessId: string; data: SocialWorkspace }) {
  const queryClient = useQueryClient();
  const [platform, setPlatform] = useState(data.channels[0]?.platformCode ?? "FACEBOOK");
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(data.items[0]?.id ?? null);
  const selected = data.items.find((item) => item.id === selectedId) ?? data.items[0] ?? null;

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["social"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  }

  const create = useMutation({
    mutationFn: () => api.createSocial(businessId, { platformCode: platform, title, body }),
    onSuccess: async (item) => {
      setError(null);
      setSuccess("Draft stored. It has not been posted to any live network.");
      setTitle("");
      setBody("");
      setSelectedId(item.id);
      await refresh();
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The draft could not be saved.");
    }
  });

  async function run(action: () => Promise<unknown>, ok: string) {
    setError(null);
    try {
      await action();
      setSuccess(ok);
      await refresh();
    } catch (err) {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The social action failed.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Google / Meta / Social</p>
        <h1 className="page-title">Social content</h1>
        <p className="page-lead">{data.note}</p>
        <div className="id-form-actions spaced">
          <Button appearance="subtle" onClick={() => void run(() => api.refreshSocialMetrics(businessId), "Metric hold-states refreshed. No live counts were invented.")}>
            Refresh metrics
          </Button>
        </div>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-3">
        {data.channels.map((channel) => (
          <article className="panel" key={channel.platformCode}>
            <p className="hero-kicker">{channel.category}</p>
            <h3>{channel.platformName}</h3>
            <div className="row-line"><span>Connection</span><span className={`sev ${channel.connectionStatus === "Connected" ? "sev-ok" : "sev-hold"}`}>{channel.connectionStatus ?? "Not connected"}</span></div>
            <div className="row-line"><span>Publish</span><span className="sev sev-hold">{channel.canPublish ? "Capability listed" : "Assisted only"}</span></div>
            <div className="row-line"><span>Metrics</span><span className={`sev ${channel.metricStatus === "Hold" ? "sev-warn" : "sev-hold"}`}>{channel.metricStatus ?? "Not captured"}</span></div>
            {channel.metricDetail ? <p className="ink-muted meta-line">{channel.metricDetail}</p> : null}
          </article>
        ))}
      </div>

      <div className="workspace-split">
        <aside className="panel draft-form">
          <h2>New draft</h2>
          <p className="hero-kicker">Channel</p>
          <div className="draft-channels" role="group" aria-label="Channel">
            {data.channels.map((channel) => (
              <Button
                key={channel.platformCode}
                appearance={platform === channel.platformCode ? "primary" : "subtle"}
                onClick={() => setPlatform(channel.platformCode)}
              >
                {channel.platformName}
              </Button>
            ))}
          </div>
          <Input value={title} onChange={(_, next) => setTitle(next.value)} placeholder="Title" aria-label="Draft title" />
          <Textarea value={body} onChange={(_, next) => setBody(next.value)} placeholder="Body" aria-label="Draft body" resize="vertical" />
          <Button appearance="primary" disabled={create.isPending} onClick={() => create.mutate()}>Save draft</Button>
        </aside>
        <div>
          {data.items.length === 0 ? (
            <div className="dp-empty dp-surface dp-empty-lg">
              <strong>No social drafts</strong>
              <p>Create a Google, Facebook, Instagram, LinkedIn, or YouTube draft. WhatsApp is not listed here.</p>
            </div>
          ) : (
            <DataGrid
              noun="draft"
              empty="Save a draft to start the approve → publish loop."
              selectedId={selected?.id}
              onRow={setSelectedId}
              columns={["Channel", "Title", "Status", "Verify"]}
              rows={data.items.map((item) => ({
                id: item.id,
                search: `${item.platformCode} ${item.title} ${item.status}`.toLowerCase(),
                cells: [item.platformCode, item.title, item.status, item.verificationStatus]
              }))}
            />
          )}
          {selected ? (
            <article className="panel spaced">
              <p className="hero-kicker">{selected.kind} · {selected.status}</p>
              <h2>{selected.title}</h2>
              <p className="ink-muted">{selected.body}</p>
              {selected.verificationDetail ? <p>{selected.verificationDetail}</p> : null}
              <div className="id-form-actions">
                {selected.status !== "Approved" ? (
                  <Button appearance="subtle" onClick={() => void run(() => api.approveSocial(businessId, selected.id), "Draft approved. Live publish is still blocked.")}>Approve</Button>
                ) : (
                  <Button appearance="primary" onClick={() => void run(() => api.publishSocial(businessId, selected.id), "Publish stayed on hold. No live post was created.")}>Publish</Button>
                )}
              </div>
            </article>
          ) : null}
        </div>
      </div>
    </section>
  );
}
