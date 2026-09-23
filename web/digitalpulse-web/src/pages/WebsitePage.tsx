import { Button, Input } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type SiteSearch, type WebsiteIntelligence } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";

export function WebsitePage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["website", businessId],
    queryFn: () => api.website(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening website intelligence" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before analyzing the website." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading website intelligence" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Website intelligence unavailable" />;

  return <WebsiteWorkspace businessId={businessId} data={query.data} />;
}

function WebsiteWorkspace({ businessId, data }: { businessId: string; data: WebsiteIntelligence }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [needle, setNeedle] = useState("");
  const [search, setSearch] = useState<SiteSearch | null>(null);
  const snapshot = data.snapshot;

  const analyze = useMutation({
    mutationFn: () => api.analyzeWebsite(businessId),
    onSuccess: async () => {
      setError(null);
      setSuccess("Homepage snapshot stored. Search Console metrics were not invented.");
      await queryClient.invalidateQueries({ queryKey: ["website"] });
      await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "Website analysis could not start.");
    }
  });

  async function runSearch() {
    setError(null);
    if (needle.trim().length < 2) {
      setError("Enter at least two characters to search the indexed website.");
      return;
    }
    try {
      setSearch(await api.searchWebsite(businessId, needle.trim()));
    } catch (err) {
      setSearch(null);
      setError(err instanceof ApiError ? err.title : "Site search failed.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Website + Search</p>
        <h1 className="display" style={{ fontSize: "clamp(2rem, 5vw, 3.4rem)", margin: 0 }}>Website intelligence</h1>
        <p style={{ color: "var(--muted)", maxWidth: "42rem" }}>
          On-page SEO and AEO come from a safe homepage fetch. {data.searchProvider} search is tenant-scoped.
          Vector search is {data.vectorSearchConfigured ? "configured" : "not configured"}. Search Console clicks are never invented.
        </p>
        <div className="id-form-actions" style={{ marginTop: "0.9rem" }}>
          <Button appearance="primary" disabled={analyze.isPending} onClick={() => analyze.mutate()}>
            {analyze.isPending ? "Analyzing…" : "Analyze website"}
          </Button>
        </div>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-3">
        <article className="panel">
          <h2>Homepage snapshot</h2>
          {!snapshot ? (
            <div className="dp-empty" style={{ minHeight: "8rem" }}>
              <strong>No snapshot yet</strong>
              <p>Set the official website on identity, then analyze. Private hosts are blocked.</p>
            </div>
          ) : (
            <>
              <div className="row-line"><span>Status</span><span className={`sev ${snapshot.status === "Reached" ? "sev-ok" : "sev-warn"}`}>{snapshot.status}</span></div>
              <div className="row-line"><span>URL</span><span>{snapshot.url ?? "—"}</span></div>
              <div className="row-line"><span>Title</span><span>{snapshot.title ?? "—"}</span></div>
              <div className="row-line"><span>H1</span><span>{snapshot.h1 ?? "—"}</span></div>
              <div className="row-line"><span>Words</span><span>{snapshot.wordCount}</span></div>
              <div className="row-line"><span>JSON-LD</span><span className={`sev ${snapshot.hasJsonLd ? "sev-ok" : "sev-hold"}`}>{snapshot.hasJsonLd ? "Present" : "Missing"}</span></div>
            </>
          )}
        </article>
        <article className="panel">
          <h2>Search Console</h2>
          <p style={{ color: "var(--muted)" }}>{data.searchConsole.detail}</p>
          <div className="row-line"><span>Status</span><span className={`sev ${data.searchConsole.status === "NotConnected" ? "sev-hold" : "sev-warn"}`}>{data.searchConsole.status}</span></div>
          <div className="row-line"><span>Grant</span><span>{data.searchConsole.grantKind ?? "—"}</span></div>
        </article>
        <article className="panel">
          <h2>Site search</h2>
          <p style={{ color: "var(--muted)" }}>Search only the HTML DigitalPulse fetched for this tenant.</p>
          <div className="id-form-actions">
            <Input value={needle} onChange={(_, next) => setNeedle(next.value)} placeholder="Search indexed copy" aria-label="Search indexed website" />
            <Button appearance="subtle" onClick={() => void runSearch()}>Search</Button>
          </div>
          {search && search.hits.length === 0 ? (
            <div className="dp-empty" style={{ minHeight: "6rem" }}>
              <strong>No indexed matches</strong>
              <p>Analyze the website first, then search words that appear on the page.</p>
            </div>
          ) : null}
          {search?.hits.map((hit) => (
            <div className="row-line" key={`${hit.url}-${hit.title}`}>
              <div>
                <strong>{hit.title}</strong>
                <p style={{ margin: "0.15rem 0 0", color: "var(--muted)" }}>{hit.snippet}</p>
              </div>
              <span className="sev sev-hold">{hit.score}</span>
            </div>
          ))}
        </article>
      </div>

      {!snapshot ? (
        <div className="dp-empty dp-surface" style={{ minHeight: "14rem" }}>
          <strong>No SEO or AEO observations</strong>
          <p>Observations appear after a successful or blocked fetch. Rankings are not estimated.</p>
        </div>
      ) : data.observations.length === 0 ? (
        <div className="dp-empty dp-surface" style={{ minHeight: "14rem" }}>
          <strong>No observations on this snapshot</strong>
          <p>The homepage did not produce on-page SEO, AEO, or Search Console gaps.</p>
        </div>
      ) : (
        <DataGrid
          noun="observation"
          empty="Analyze the website to produce SEO and AEO observations."
          columns={["Severity", "Observation", "Category"]}
          rows={data.observations.map((item) => ({
            id: item.id,
            search: `${item.title} ${item.category} ${item.severity}`.toLowerCase(),
            cells: [
              <span className={`sev ${item.severity === "High" ? "sev-crit" : item.severity === "Medium" ? "sev-warn" : "sev-hold"}`} key="sev">{item.severity}</span>,
              item.title,
              item.category
            ]
          }))}
        />
      )}

      {data.observations[0] ? (
        <article className="panel" style={{ marginTop: "1rem" }}>
          <p className="hero-kicker">{data.observations[0].category}</p>
          <h2>{data.observations[0].title}</h2>
          <p style={{ color: "var(--muted)" }}>{data.observations[0].detail}</p>
          <div className="row-line"><span>Expected</span><span>{data.observations[0].expectedValue ?? "—"}</span></div>
          <div className="row-line"><span>Observed</span><span>{data.observations[0].observedValue ?? "—"}</span></div>
          <div className="row-line"><span>Recommendation</span><span>{data.observations[0].recommendation}</span></div>
        </article>
      ) : null}
    </section>
  );
}
