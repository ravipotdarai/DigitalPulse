import { Textarea } from "@fluentui/react-components";
import { Button } from "../design/Button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type AiRun, type AiWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";
import { Field } from "../design/Field";
import { AIBrief } from "../design/AIBrief";
import { PageHeader } from "../design/PageHeader";

export function AiPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["ai", businessId],
    queryFn: () => api.ai(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening the orchestrator" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before asking DigitalPulse to reason from the record." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading Graphify and knowledge" />;
  if (query.isError || !query.data) return <PageState mode="error" title="AI workspace unavailable" />;

  return <AiWorkspaceView businessId={businessId} data={query.data} />;
}

function AiWorkspaceView({ businessId, data }: { businessId: string; data: AiWorkspace }) {
  const queryClient = useQueryClient();
  const [agent, setAgent] = useState(data.agents[0]?.code ?? "research");
  const [prompt, setPrompt] = useState("");
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [kind, setKind] = useState("Note");
  const [sourceUrl, setSourceUrl] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(data.runs[0]?.id ?? null);
  const selected = data.runs.find((run) => run.id === selectedId) ?? data.runs[0] ?? null;

  async function refresh(nextId?: string) {
    await queryClient.invalidateQueries({ queryKey: ["ai"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    if (nextId) setSelectedId(nextId);
  }

  const addKnowledge = useMutation({
    mutationFn: () => api.addKnowledge(businessId, { title, body, kind, sourceUrl: sourceUrl || null }),
    onSuccess: async () => {
      setError(null);
      setSuccess("Knowledge stored and indexed for this tenant. It is not a live web scrape.");
      setTitle("");
      setBody("");
      setSourceUrl("");
      await refresh();
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The knowledge entry could not be saved.");
    }
  });

  const sync = useMutation({
    mutationFn: () => api.syncGraph(businessId),
    onSuccess: async () => {
      setError(null);
      setSuccess("Graphify rebuilt from the identity record, projects, findings, and knowledge.");
      await refresh();
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "Graphify could not be rebuilt.");
    }
  });

  const run = useMutation({
    mutationFn: () => api.runAi(businessId, { agent, prompt }),
    onSuccess: async (result) => {
      setError(null);
      setSuccess(result.holdReason || "Run stored. Nothing was published.");
      setPrompt("");
      await refresh(result.id);
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The orchestrator could not complete the run.");
    }
  });

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">AI Orchestrator</p>
        <h1 className="display display-page command-title">Reason from the record</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          Provider <b>{data.providerName}</b> · {data.providerIsLive ? "live" : "development hold"} · {data.nodes.length} Graphify nodes · {data.knowledge.length} knowledge entries
        </p>
        <div className="id-form-actions spaced">
          <Button appearance="subtle" disabled={sync.isPending} onClick={() => sync.mutate()}>
            {sync.isPending ? "Rebuilding…" : "Rebuild Graphify"}
          </Button>
        </div>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Ask an agent</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              run.mutate();
            }}
          >
            <label className="dp-field">
              <span>Agent</span>
              <select value={agent} onChange={(event) => setAgent(event.target.value)}>
                {data.agents.map((item) => (
                  <option key={item.code} value={item.code}>{item.name}</option>
                ))}
              </select>
            </label>
            <p className="ink-muted">{data.agents.find((item) => item.code === agent)?.purpose}</p>
            <label className="dp-field">
              <span>Ask</span>
              <Textarea appearance="outline" resize="vertical" value={prompt} onChange={(_, d) => setPrompt(d.value)} required />
            </label>
            <Button appearance="primary" type="submit" disabled={run.isPending || prompt.trim().length < 4}>
              {run.isPending ? "Orchestrating…" : "Run orchestrator"}
            </Button>
          </form>
        </article>
        <article className="panel">
          <h2>Knowledge base</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              addKnowledge.mutate();
            }}
          >
            <Field label="Title" value={title} onChange={setTitle} required />
            <label className="dp-field">
              <span>Kind</span>
              <select value={kind} onChange={(event) => setKind(event.target.value)}>
                <option value="Note">Note</option>
                <option value="Document">Document</option>
                <option value="Url">Url</option>
              </select>
            </label>
            <Field label="Source URL" value={sourceUrl} onChange={setSourceUrl} type="url" />
            <label className="dp-field">
              <span>Body</span>
              <Textarea appearance="outline" resize="vertical" value={body} onChange={(_, d) => setBody(d.value)} required />
            </label>
            <Button appearance="primary" type="submit" disabled={addKnowledge.isPending || !title.trim() || !body.trim()}>
              {addKnowledge.isPending ? "Indexing…" : "Add knowledge"}
            </Button>
          </form>
        </article>
      </div>

      <div className="band band-2">
        <article className="panel">
          <h2>Runs</h2>
          <DataGrid
            noun="run"
            maxHeight="20rem"
            empty="Run the orchestrator after the identity record has something to retrieve."
            columns={["Agent", "Status", "Confidence", "Provider"]}
            rows={data.runs.map((item) => ({
              id: item.id,
              search: `${item.agent} ${item.prompt}`.toLowerCase(),
              cells: [item.agent, item.status, item.confidence, item.providerIsLive ? item.providerName : `${item.providerName} (held)`],
              actions: <button type="button" className="grid-action" onClick={() => setSelectedId(item.id)}>Open</button>
            }))}
            onRow={(id) => setSelectedId(id)}
          />
        </article>
        <article className="panel">
          <h2>Graphify nodes</h2>
          <DataGrid
            noun="node"
            maxHeight="20rem"
            empty="Rebuild Graphify from the identity record."
            columns={["Kind", "Label", "Value"]}
            rows={data.nodes.map((node) => ({
              id: node.id,
              search: `${node.kind} ${node.label} ${node.value ?? ""}`.toLowerCase(),
              cells: [node.kind, node.label, node.value ?? "—"]
            }))}
          />
        </article>
      </div>

      {selected ? <RunDetail run={selected} /> : null}

      {data.knowledge.length === 0 ? (
        <PageState mode="empty" title="No knowledge entries yet" detail="Add a note, document, or URL that this business can stand behind." />
      ) : (
        <article className="panel">
          <h2>Indexed knowledge</h2>
          {data.knowledge.map((entry) => (
            <div className="row-line" key={entry.id}>
              <span>{entry.title}</span>
              <span className="sev sev-hold">{entry.kind}</span>
            </div>
          ))}
        </article>
      )}
    </section>
  );
}

function RunDetail({ run }: { run: AiRun }) {
  const evaluation = run.evaluation;
  return (
    <section className="band band-2">
      <AIBrief
        title={`${run.agent} · ${run.status}`}
        steps={[
          { label: "Observed", value: run.prompt, tone: run.status === "Rejected" ? "failing" : run.status === "Held" ? "warning" : "live" },
          { label: "Evidence", value: evaluation ? (evaluation.hasEvidence ? "Retrieved from Graphify, facts, and the knowledge base." : "No evidence.") : "Evaluation pending." },
          { label: "Recommendation", value: run.output },
          { label: "Action", value: run.holdReason, tone: run.status === "Completed" ? "live" : "idle" },
          { label: "Result", value: `${run.confidence} confidence · ${run.providerName}`, tone: run.providerIsLive ? "live" : "idle" }
        ]}
        provenance={
          run.providerIsLive
            ? "Live provider output was validated against retrieved evidence. Nothing was published."
            : "Development composition from retrieved evidence. No language model generated this text."
        }
      />
      <article className="panel">
        <h2>Evaluation + audit</h2>
        {evaluation ? (
          <>
            <div className="row-line"><span>Passed</span><span>{evaluation.passed ? "Yes" : "No"}</span></div>
            <div className="row-line"><span>Conflict</span><span>{evaluation.hasConflict ? "Review required" : "None"}</span></div>
            <div className="row-line"><span>Restricted</span><span>{evaluation.hasRestrictedFact ? "Withheld from publish" : "None in output"}</span></div>
            <p className="ink-muted">{evaluation.summary}</p>
          </>
        ) : null}
        {run.audit.map((event) => (
          <div className="row-line" key={`${event.stage}-${event.atUtc}`}>
            <span>{event.stage}</span>
            <span>{event.detail}</span>
          </div>
        ))}
      </article>
    </section>
  );
}
