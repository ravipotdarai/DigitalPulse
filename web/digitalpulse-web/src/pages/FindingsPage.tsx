import { Button } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { ApiError, api, type Finding, type ScanCenter, type ScanDetail } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";

export function FindingsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["scans", businessId],
    queryFn: () => api.scans(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening DigitalPulse Check" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before running a check." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading scans" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Findings unavailable" />;

  return <CheckWorkspace businessId={businessId} center={query.data} />;
}

function CheckWorkspace({ businessId, center }: { businessId: string; center: ScanCenter }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(center.latest?.findings[0]?.id ?? null);
  const [scanId, setScanId] = useState<string | null>(center.latest?.id ?? null);

  const selectedScan = useQuery({
    queryKey: ["scan", businessId, scanId],
    queryFn: () => api.scan(businessId, scanId!),
    enabled: Boolean(scanId) && scanId !== center.latest?.id
  });

  const detail: ScanDetail | null = scanId && scanId !== center.latest?.id
    ? selectedScan.data ?? null
    : center.latest;

  const findings = detail?.findings ?? [];
  const selected = useMemo(
    () => findings.find((item) => item.id === selectedId) ?? findings[0] ?? null,
    [findings, selectedId]
  );

  const run = useMutation({
    mutationFn: () => api.runScan(businessId),
    onSuccess: async (scan) => {
      setError(null);
      setScanId(scan.id);
      setSelectedId(scan.findings[0]?.id ?? null);
      await queryClient.invalidateQueries({ queryKey: ["scans"] });
      await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (err) => setError(err instanceof ApiError ? err.title : "DigitalPulse Check could not start.")
  });

  async function setStatus(finding: Finding, status: string) {
    setError(null);
    try {
      await api.updateFinding(businessId, finding.id, status);
      await queryClient.invalidateQueries({ queryKey: ["scans"] });
      await queryClient.invalidateQueries({ queryKey: ["scan"] });
      await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not update the finding.");
    }
  }

  const remaining = Math.max(0, center.scansPerMonth - center.scansUsedThisMonth);

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">DigitalPulse Check</p>
        <h1 className="display" style={{ fontSize: "clamp(2rem, 5vw, 3.4rem)", margin: 0 }}>Findings</h1>
        <p style={{ color: "var(--muted)", maxWidth: "40rem" }}>
          Each check compares the canonical identity to the official website and to authorized connections.
          Development grants do not invent live Google or Meta listings. {center.scansUsedThisMonth} / {center.scansPerMonth} scans used this month.
        </p>
        <div className="id-form-actions" style={{ marginTop: "0.9rem" }}>
          <Button appearance="primary" disabled={run.isPending || remaining === 0} onClick={() => run.mutate()}>
            {run.isPending ? "Running check…" : remaining === 0 ? "Monthly scan limit reached" : "Run DigitalPulse Check"}
          </Button>
        </div>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
      </header>

      {center.scans.length === 0 ? (
        <div className="dp-empty dp-surface" style={{ minHeight: "18rem" }}>
          <strong>No scan has run</strong>
          <p>Run DigitalPulse Check after identity and connections are in place. Findings stay empty until evidence exists.</p>
        </div>
      ) : (
        <div className="workspace-split">
          <aside className="panel">
            <h2>Scans</h2>
            {center.scans.map((scan) => (
              <button
                key={scan.id}
                type="button"
                className="row-line"
                style={{ width: "100%", textAlign: "left", background: scan.id === (detail?.id ?? center.latest?.id) ? "var(--signal-soft)" : "transparent", border: 0, cursor: "pointer" }}
                onClick={() => {
                  setScanId(scan.id);
                  setSelectedId(null);
                }}
              >
                <span>
                  <strong>{scan.status}</strong>
                  <p style={{ margin: 0, color: "var(--muted)" }}>{scan.summary ?? "In progress"}</p>
                </span>
                <span className={`sev ${scan.criticalCount ? "sev-crit" : scan.openCount ? "sev-warn" : "sev-ok"}`}>
                  {scan.findingCount}
                </span>
              </button>
            ))}
          </aside>

          <div>
            {selectedScan.isLoading && scanId !== center.latest?.id ? (
              <PageState mode="loading" title="Opening scan" />
            ) : selectedScan.isError && scanId !== center.latest?.id ? (
              <PageState mode="error" title="Scan unavailable" />
            ) : findings.length === 0 ? (
              <div className="dp-empty dp-surface" style={{ minHeight: "16rem" }}>
                <strong>No findings on this scan</strong>
                <p>Identity, website, and authorized platforms did not produce evidence-backed issues.</p>
              </div>
            ) : (
              <>
                <DataGrid
                  noun="finding"
                  empty="Run DigitalPulse Check to produce findings."
                  selectedId={selected?.id}
                  onRow={setSelectedId}
                  columns={["Severity", "Finding", "Category", "Status"]}
                  rows={findings.map((item) => ({
                    id: item.id,
                    search: `${item.title} ${item.category} ${item.severity} ${item.status}`.toLowerCase(),
                    cells: [
                      <span className={`sev ${severityTone(item.severity)}`} key="sev">{item.severity}</span>,
                      item.title,
                      item.category,
                      item.status
                    ]
                  }))}
                />

                {selected ? (
                  <article className="panel" style={{ marginTop: "1rem" }}>
                    <p className="hero-kicker">{selected.category} · {selected.automationState}</p>
                    <h2>{selected.title}</h2>
                    <p style={{ color: "var(--muted)" }}>{selected.description}</p>
                    <div className="row-line"><span>Expected</span><span>{selected.expectedValue ?? "—"}</span></div>
                    <div className="row-line"><span>Observed</span><span>{selected.observedValue ?? "—"}</span></div>
                    <div className="row-line"><span>Recommendation</span><span>{selected.recommendation}</span></div>
                    <div className="row-line"><span>Suggested action</span><span>{selected.suggestedAction}</span></div>
                    <div className="row-line"><span>Verify</span><span>{selected.verificationMethod}</span></div>
                    <h3 style={{ marginTop: "1rem" }}>Evidence</h3>
                    {selected.evidence.length === 0 ? (
                      <p style={{ color: "var(--muted)" }}>This finding is missing evidence and should not have been stored.</p>
                    ) : selected.evidence.map((item) => (
                      <div className="row-line" key={item.id}>
                        <div>
                          <strong>{item.label}</strong>
                          <p style={{ margin: "0.15rem 0 0", color: "var(--muted)" }}>{item.value}</p>
                        </div>
                        <span className="sev sev-hold">{item.source}</span>
                      </div>
                    ))}
                    <div className="id-form-actions" style={{ marginTop: "0.9rem" }}>
                      {selected.status !== "Acknowledged" ? (
                        <Button appearance="subtle" onClick={() => void setStatus(selected, "Acknowledged")}>Acknowledge</Button>
                      ) : null}
                      {selected.status !== "Resolved" ? (
                        <Button appearance="subtle" onClick={() => void setStatus(selected, "Resolved")}>Resolve</Button>
                      ) : null}
                      {selected.status !== "Open" ? (
                        <Button appearance="subtle" onClick={() => void setStatus(selected, "Open")}>Reopen</Button>
                      ) : null}
                    </div>
                  </article>
                ) : null}
              </>
            )}
          </div>
        </div>
      )}
    </section>
  );
}

function severityTone(severity: string) {
  if (severity === "Critical" || severity === "High") return "sev-crit";
  if (severity === "Medium") return "sev-warn";
  if (severity === "Low") return "sev-hold";
  return "sev-ok";
}
