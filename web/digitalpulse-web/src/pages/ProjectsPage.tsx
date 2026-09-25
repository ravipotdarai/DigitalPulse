import { Input, Textarea } from "@fluentui/react-components";
import { Button } from "../design/Button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type ProjectDetail, type ProjectWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";
import { Field } from "../design/Field";

export function ProjectsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["projects", businessId],
    queryFn: () => api.projects(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening projects" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before recording a project." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading projects" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Project workspace unavailable" />;

  return <ProjectWorkspaceView businessId={businessId} data={query.data} />;
}

function ProjectWorkspaceView({ businessId, data }: { businessId: string; data: ProjectWorkspace }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [clientName, setClientName] = useState("");
  const [description, setDescription] = useState("");
  const [outcomes, setOutcomes] = useState("");
  const [permissionScope, setPermissionScope] = useState("Partial");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(data.projects[0]?.id ?? null);
  const detail = useQuery({
    queryKey: ["project", businessId, selectedId],
    queryFn: () => api.project(businessId, selectedId!),
    enabled: Boolean(selectedId)
  });

  async function refresh(nextId?: string) {
    await queryClient.invalidateQueries({ queryKey: ["projects"] });
    await queryClient.invalidateQueries({ queryKey: ["project"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    if (nextId) setSelectedId(nextId);
  }

  const create = useMutation({
    mutationFn: () => api.createProject(businessId, {
      name,
      clientName: clientName || null,
      industry: null,
      location: null,
      description: description || null,
      outcomes: outcomes || null,
      startedOn: null,
      completedOn: null,
      permissionScope,
      confidentiality: "Internal"
    }),
    onSuccess: async (project) => {
      setError(null);
      setSuccess("Project stored. Nothing has been published.");
      setName("");
      setClientName("");
      setDescription("");
      setOutcomes("");
      await refresh(project.project.id);
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The project could not be saved.");
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
      setError(err instanceof ApiError ? err.title : "The project action failed.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Projects + Content</p>
        <h1 className="display display-page command-title">Project factory</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>New project</h2>
          <form className="id-form" onSubmit={(event) => { event.preventDefault(); create.mutate(); }}>
            <Field label="Name" value={name} onChange={setName} required />
            <Field label="Client" value={clientName} onChange={setClientName} />
            <label className="dp-field">
              <span>Permission</span>
              <select value={permissionScope} onChange={(event) => setPermissionScope(event.target.value)}>
                <option value="None">None</option>
                <option value="Partial">Partial</option>
                <option value="Full">Full</option>
              </select>
            </label>
            <label className="dp-field">
              <span>Description</span>
              <Textarea value={description} onChange={(_, d) => setDescription(d.value)} />
            </label>
            <label className="dp-field">
              <span>Outcomes</span>
              <Textarea value={outcomes} onChange={(_, d) => setOutcomes(d.value)} />
            </label>
            <Button appearance="primary" type="submit" disabled={create.isPending || !name.trim()}>
              {create.isPending ? "Saving…" : "Save project"}
            </Button>
          </form>
        </article>
        <article>
          <DataGrid
            noun="project"
            empty="Record a completed engagement before generating variants."
            columns={["Project", "Client", "Permission", "Status"]}
            rows={data.projects.map((project) => ({
              id: project.id,
              search: `${project.name} ${project.clientName ?? ""}`.toLowerCase(),
              cells: [project.name, project.clientName ?? "—", project.permissionScope, project.publicationStatus],
              actions: <button type="button" className="grid-action" onClick={() => setSelectedId(project.id)}>Open</button>
            }))}
            onRow={(id) => setSelectedId(id)}
          />
        </article>
      </div>

      {selectedId && detail.isLoading ? <PageState mode="loading" title="Opening project" /> : null}
      {selectedId && detail.isError ? <PageState mode="error" title="Project unavailable" /> : null}
      {detail.data ? (
        <ProjectDetailView
          businessId={businessId}
          workspace={data}
          detail={detail.data}
          onAction={run}
        />
      ) : null}
    </section>
  );
}

function ProjectDetailView({
  businessId,
  workspace,
  detail,
  onAction
}: {
  businessId: string;
  workspace: ProjectWorkspace;
  detail: ProjectDetail;
  onAction: (action: () => Promise<unknown>, ok: string) => Promise<void>;
}) {
  const projectId = detail.project.id;
  const [mediaLabel, setMediaLabel] = useState("");
  const [mediaUrl, setMediaUrl] = useState("");
  const openApproval = detail.packs.flatMap((pack) => pack.approvals).find((item) => item.open);

  return (
    <section className="band band-2">
      <article className="panel">
        <p className="hero-kicker">{detail.project.confidentiality}</p>
        <h2>{detail.project.name}</h2>
        <p className="ink-muted">{detail.description ?? "No description stored."}</p>
        <div className="row-line"><span>Client</span><span>{detail.project.clientName ?? "—"}</span></div>
        <div className="row-line"><span>Permission</span><span className="sev sev-hold">{detail.project.permissionScope}</span></div>
        <div className="row-line"><span>Services</span><span>{detail.services.join(", ") || "None linked"}</span></div>
        <div className="row-line"><span>Brands</span><span>{detail.brands.join(", ") || "None linked"}</span></div>
        <div className="id-form-actions">
          {workspace.services.map((service) => (
            <Button key={service.id} appearance="subtle" onClick={() => void onAction(() => api.linkProjectService(businessId, projectId, service.id), "Service linked.")}>
              {service.name}
            </Button>
          ))}
        </div>
        <div className="id-form-actions">
          <Input value={mediaLabel} onChange={(_, d) => setMediaLabel(d.value)} placeholder="Media label" />
          <Input value={mediaUrl} onChange={(_, d) => setMediaUrl(d.value)} placeholder="Source URL (optional)" />
          <Button
            appearance="subtle"
            disabled={!mediaLabel.trim()}
            onClick={() => void onAction(() => api.registerProjectMedia(businessId, projectId, { label: mediaLabel, kind: "Image", sourceUrl: mediaUrl || null }), "Media catalogued. No file was uploaded to a CDN.")}
          >
            Register media
          </Button>
          <Button appearance="primary" onClick={() => void onAction(() => api.generateProjectContent(businessId, projectId), "Content pack assembled from the project record.")}>
            Run content factory
          </Button>
        </div>
        {detail.media.length === 0 ? <p className="ink-muted">No media records yet.</p> : detail.media.map((item) => (
          <div className="row-line" key={item.id}><span>{item.label}</span><span className="sev sev-hold">{item.kind}</span></div>
        ))}
      </article>
      <article className="panel">
        <h2>Content packs</h2>
        {detail.packs.length === 0 ? (
          <div className="dp-empty dp-empty-sm">
            <strong>No factory run yet</strong>
            <p>Variants are generated from stored project fields, not a live AI provider.</p>
          </div>
        ) : (
          detail.packs.map((pack) => (
            <article className="panel" key={pack.id}>
              <h3>{pack.title}</h3>
              <p className="ink-muted">{pack.sourceNote}</p>
              <div className="row-line"><span>Status</span><span className={`sev ${pack.status === "Approved" ? "sev-ok" : pack.status === "Hold" ? "sev-warn" : "sev-hold"}`}>{pack.status}</span></div>
              {pack.variants.map((variant) => (
                <div className="row-line" key={variant.id}>
                  <span>{variant.kind}</span>
                  <span className={`sev ${variant.status === "Approved" ? "sev-ok" : "sev-hold"}`}>{variant.status}</span>
                </div>
              ))}
              <div className="id-form-actions">
                <Button appearance="subtle" onClick={() => void onAction(() => api.requestProjectApproval(businessId, projectId, pack.id), "Approval requested.")}>
                  Request approval
                </Button>
              </div>
            </article>
          ))
        )}
        {openApproval ? (
          <div className="id-form-actions">
            <Button appearance="primary" onClick={() => void onAction(() => api.decideProjectApproval(businessId, projectId, openApproval.id, true, "Approved against stored permission scope."), "Decision recorded. Live publish stays on hold.")}>
              Approve pack
            </Button>
            <Button appearance="subtle" onClick={() => void onAction(() => api.decideProjectApproval(businessId, projectId, openApproval.id, false, "Rejected. Variants stay unpublished."), "Pack rejected.")}>
              Reject
            </Button>
          </div>
        ) : null}
      </article>
    </section>
  );
}
