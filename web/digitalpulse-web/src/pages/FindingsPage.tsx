import { Button } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, motion } from "framer-motion";
import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiError, api, type Finding, type ScanCenter, type ScanDetail } from "../lib/api";
import { PageState } from "../components/PageState";
import { AIBrief } from "../design/AIBrief";
import { MOTION, Stagger, StaggerItem, useMotionTiming } from "../design/motion";
import { Meter, PageHeader, SectionTitle } from "../design/PageHeader";
import { relativeTime, severityRank } from "../design/platforms";
import { SignalGlyph, SignalRow } from "../design/Signal";

type SeverityFilter = "all" | "high" | "medium" | "low";
type StatusFilter = "active" | "all" | "Resolved";

export function FindingsPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["scans", businessId],
    queryFn: () => api.scans(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening signals" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before running a check." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading signals" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Signals unavailable" />;

  return <SignalsWorkspace businessId={businessId} center={query.data} />;
}

function SignalsWorkspace({ businessId, center }: { businessId: string; center: ScanCenter }) {
  const queryClient = useQueryClient();
  const [params, setParams] = useSearchParams();
  const { reduce, base } = useMotionTiming();
  const [error, setError] = useState<string | null>(null);
  const [scanId, setScanId] = useState<string | null>(center.latest?.id ?? null);
  const [severity, setSeverity] = useState<SeverityFilter>("all");
  const [status, setStatusFilter] = useState<StatusFilter>("active");
  const [updating, setUpdating] = useState<string | null>(null);

  const selectedScan = useQuery({
    queryKey: ["scan", businessId, scanId],
    queryFn: () => api.scan(businessId, scanId!),
    enabled: Boolean(scanId) && scanId !== center.latest?.id
  });
  const detail: ScanDetail | null = scanId && scanId !== center.latest?.id ? selectedScan.data ?? null : center.latest;
  const all = useMemo(
    () => [...(detail?.findings ?? [])].sort((a, b) => severityRank(b.severity) - severityRank(a.severity)),
    [detail]
  );

  const counts = {
    all: all.length,
    high: all.filter((f) => severityRank(f.severity) >= 3).length,
    medium: all.filter((f) => severityRank(f.severity) === 2).length,
    low: all.filter((f) => severityRank(f.severity) <= 1).length
  };

  const visible = all.filter((finding) => {
    const rank = severityRank(finding.severity);
    const severityOk =
      severity === "all" || (severity === "high" && rank >= 3) || (severity === "medium" && rank === 2) || (severity === "low" && rank <= 1);
    const statusOk = status === "all" || (status === "active" ? finding.status !== "Resolved" : finding.status === "Resolved");
    return severityOk && statusOk;
  });

  const selectedId = params.get("signal");
  const selected = visible.find((item) => item.id === selectedId) ?? visible[0] ?? null;
  const selectedIndex = selected ? visible.indexOf(selected) + 1 : 0;

  function select(id: string) {
    const next = new URLSearchParams(params);
    next.set("signal", id);
    setParams(next, { replace: true });
  }

  const run = useMutation({
    mutationFn: () => api.runScan(businessId),
    onSuccess: async (scan) => {
      setError(null);
      setScanId(scan.id);
      if (scan.findings[0]) select(scan.findings[0].id);
      await queryClient.invalidateQueries({ queryKey: ["scans"] });
      await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (err) => setError(err instanceof ApiError ? err.title : "DigitalPulse Check could not start.")
  });

  async function setStatus(finding: Finding, next: string) {
    setError(null);
    setUpdating(next);
    try {
      await api.updateFinding(businessId, finding.id, next);
      await queryClient.invalidateQueries({ queryKey: ["scans"] });
      await queryClient.invalidateQueries({ queryKey: ["scan"] });
      await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not update the signal.");
    } finally {
      setUpdating(null);
    }
  }

  const remaining = Math.max(0, center.scansPerMonth - center.scansUsedThisMonth);

  return (
    <div className="page-view">
      <PageHeader
        kicker="DigitalPulse Check"
        title="Signals"
        lead="Each check compares the canonical identity to the official website and to authorized connections. A signal exists only when evidence does. Development grants never invent live Google or Meta listings."
        actions={
          <Button appearance="primary" disabled={run.isPending || remaining === 0} onClick={() => run.mutate()}>
            {run.isPending ? "Checking the presence…" : remaining === 0 ? "Monthly check limit reached" : "Run DigitalPulse Check"}
          </Button>
        }
        aside={<Meter label="Checks this month" value={center.scansUsedThisMonth} max={center.scansPerMonth} />}
      >
        {run.isPending ? <div className="scanning" role="progressbar" aria-label="DigitalPulse Check running" /> : null}
        {error ? <p className="note-err" role="alert">{error}</p> : null}
      </PageHeader>

      {center.scans.length === 0 ? (
        <p className="empty-line">
          <strong>No check has run</strong>
          Run DigitalPulse Check after identity and connections are in place. Signals stay empty until evidence exists.
        </p>
      ) : (
        <>
          <ol className="scan-rail" aria-label="Check history">
            {center.scans.map((scan) => (
              <li key={scan.id}>
                <button
                  type="button"
                  aria-pressed={scan.id === (detail?.id ?? center.latest?.id)}
                  onClick={() => setScanId(scan.id)}
                >
                  <strong className="num">{scan.findingCount}</strong>
                  <small>{scan.criticalCount ? `${scan.criticalCount} critical · ` : ""}{relativeTime(scan.completedAtUtc ?? scan.startedAtUtc)}</small>
                </button>
              </li>
            ))}
          </ol>

          {selectedScan.isLoading && scanId !== center.latest?.id ? (
            <PageState mode="loading" title="Opening check" />
          ) : selectedScan.isError && scanId !== center.latest?.id ? (
            <PageState mode="error" title="Check unavailable" />
          ) : (
            <div className="sig">
              <section aria-label="Signal list">
                <div className="sig-filters" role="group" aria-label="Severity">
                  <Chip on={severity === "all"} onClick={() => setSeverity("all")} label="All" count={counts.all} />
                  <Chip on={severity === "high"} onClick={() => setSeverity("high")} label="High" count={counts.high} />
                  <Chip on={severity === "medium"} onClick={() => setSeverity("medium")} label="Medium" count={counts.medium} />
                  <Chip on={severity === "low"} onClick={() => setSeverity("low")} label="Low" count={counts.low} />
                </div>
                <div className="sig-filters" role="group" aria-label="Status">
                  <Chip on={status === "active"} onClick={() => setStatusFilter("active")} label="Unresolved" />
                  <Chip on={status === "Resolved"} onClick={() => setStatusFilter("Resolved")} label="Resolved" />
                  <Chip on={status === "all"} onClick={() => setStatusFilter("all")} label="Everything" />
                </div>
                {visible.length === 0 ? (
                  <p className="empty-line">
                    <strong>{all.length === 0 ? "No signals on this check" : "Nothing matches"}</strong>
                    {all.length === 0
                      ? "Identity, website, and authorized platforms did not produce evidence-backed issues."
                      : "Change the filters to see other signals."}
                  </p>
                ) : (
                  <Stagger key={`${detail?.id}-${severity}-${status}`} className="signal-list">
                    {visible.map((finding, index) => (
                      <StaggerItem key={finding.id}>
                        <SignalRow
                          index={index + 1}
                          severity={finding.severity}
                          title={finding.title}
                          meta={`${finding.category} · ${finding.evidence.length} evidence`}
                          status={finding.status}
                          selected={finding.id === selected?.id}
                          onSelect={() => select(finding.id)}
                        />
                      </StaggerItem>
                    ))}
                  </Stagger>
                )}
              </section>

              <AnimatePresence mode="wait">
                {selected ? (
                  <motion.article
                    key={selected.id}
                    className="sig-case"
                    aria-label={`Signal ${selectedIndex}: ${selected.title}`}
                    initial={reduce ? false : { opacity: 0, y: MOTION.distance.sm }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={reduce ? undefined : { opacity: 0, y: -MOTION.distance.xs }}
                    transition={{ duration: base }}
                  >
                    <header className="sig-case-head">
                      <p className="sig-case-num">
                        <SignalGlyph severity={selected.severity} />
                        Signal <b>{String(selectedIndex).padStart(2, "0")}</b> · {selected.category} · {selected.severity}
                      </p>
                      <h2>{selected.title}</h2>
                      <p className="sig-case-lead">{selected.description}</p>
                    </header>

                    {selected.expectedValue || selected.observedValue ? (
                      <div className="sig-compare" aria-label="Expected versus observed">
                        <div className="sig-source">
                          <span>Identity record</span>
                          <strong>{selected.expectedValue ?? "Not recorded"}</strong>
                        </div>
                        <div className={selected.expectedValue === selected.observedValue ? "sig-conflict is-match" : "sig-conflict"} aria-hidden="true">
                          <svg viewBox="0 0 40 10"><path d="M0 5 H40" /></svg>
                          {selected.expectedValue === selected.observedValue ? "match" : "conflict"}
                        </div>
                        <div className={selected.expectedValue === selected.observedValue ? "sig-source is-observed is-match" : "sig-source is-observed"}>
                          <span>Observed</span>
                          <strong>{selected.observedValue ?? "Not found"}</strong>
                        </div>
                      </div>
                    ) : null}

                    <AIBrief
                      title="Analysis"
                      steps={[
                        { label: "Why it matters", value: selected.description },
                        { label: "Recommendation", value: selected.recommendation },
                        { label: "Action", value: selected.suggestedAction },
                        { label: "Verify", value: selected.verificationMethod },
                        {
                          label: "Result",
                          value: `${selected.status} · ${selected.automationState}`,
                          tone: selected.status === "Resolved" ? "live" : selected.status === "Acknowledged" ? "warning" : "idle"
                        }
                      ]}
                      provenance="Assembled from the check's stored evidence and rules. No language model generated this text."
                      footer={
                        <>
                          {selected.status !== "Acknowledged" ? (
                            <Button appearance="subtle" disabled={updating !== null} onClick={() => void setStatus(selected, "Acknowledged")}>
                              {updating === "Acknowledged" ? "Saving…" : "Acknowledge"}
                            </Button>
                          ) : null}
                          {selected.status !== "Resolved" ? (
                            <Button appearance="primary" disabled={updating !== null} onClick={() => void setStatus(selected, "Resolved")}>
                              {updating === "Resolved" ? "Saving…" : "Mark resolved"}
                            </Button>
                          ) : null}
                          {selected.status !== "Open" ? (
                            <Button appearance="subtle" disabled={updating !== null} onClick={() => void setStatus(selected, "Open")}>
                              {updating === "Open" ? "Saving…" : "Reopen"}
                            </Button>
                          ) : null}
                        </>
                      }
                    />

                    <section>
                      <SectionTitle kicker={`${selected.evidence.length} item${selected.evidence.length === 1 ? "" : "s"}`} title="Evidence" />
                      {selected.evidence.length === 0 ? (
                        <p className="empty-line">This signal has no evidence and should not have been stored.</p>
                      ) : (
                        <div className="sig-evidence">
                          {selected.evidence.map((item) => (
                            <div key={item.id}>
                              <span>
                                <strong>{item.label}</strong>
                                <p>{item.value}</p>
                              </span>
                              <em>{item.source}</em>
                            </div>
                          ))}
                        </div>
                      )}
                    </section>
                  </motion.article>
                ) : null}
              </AnimatePresence>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function Chip({ on, onClick, label, count }: { on: boolean; onClick: () => void; label: string; count?: number }) {
  return (
    <button type="button" className="chip" aria-pressed={on} onClick={onClick}>
      {label}
      {count !== undefined ? <b>{count}</b> : null}
    </button>
  );
}
