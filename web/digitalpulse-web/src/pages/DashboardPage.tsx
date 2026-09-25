import { Button } from "../design/Button";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type BusinessIdentity, type DashboardResponse, type Finding } from "../lib/api";
import { PageState } from "../components/PageState";
import { AIBrief, type BriefStep } from "../design/AIBrief";
import { DataGrid } from "../design/DataGrid";
import { CORE_LOOP, LoopTrack, type LoopStage, type StageState } from "../design/LoopTrack";
import { Reveal, Stagger, StaggerItem } from "../design/motion";
import { SectionTitle } from "../design/PageHeader";
import { PlatformEcosystem } from "../design/PlatformEcosystem";
import { linkState, relativeTime, severityRank } from "../design/platforms";
import { BrandMark } from "../design/BrandMark";
import { PulseMeter } from "../design/PulseMeter";
import { SignalRow } from "../design/Signal";
import { TiltCard } from "../design/TiltCard";
import { tenantTypeLabel } from "../lib/tenant";

export function DashboardPage() {
  const navigate = useNavigate();
  const [stage, setStage] = useState<LoopStage | null>(null);
  const query = useQuery({ queryKey: ["dashboard"], queryFn: api.dashboard });
  const firstId = query.data?.businesses[0]?.id;
  const identity = useQuery({ queryKey: ["identity", firstId], queryFn: () => api.identity(firstId!), enabled: Boolean(firstId) });
  const connections = useQuery({ queryKey: ["connections", firstId], queryFn: () => api.connections(firstId!), enabled: Boolean(firstId) });
  const scans = useQuery({ queryKey: ["scans", firstId], queryFn: () => api.scans(firstId!), enabled: Boolean(firstId) });

  if (query.isLoading) return <PageState mode="loading" title="Reading the pulse" />;
  if (query.isError || !query.data) {
    return <PageState mode="error" title="Command center unavailable" detail="Finish onboarding if you have not selected a plan." />;
  }

  const data = query.data;
  const business = data.businesses[0];
  const coverage = identity.data ? scoreIdentity(identity.data) : 0;
  const catalog = connections.data?.catalog ?? [];
  const links = connections.data?.connections ?? [];
  const linked = links.filter((link) => linkState(link) === "live").length;
  const findings = (scans.data?.latest?.findings ?? [])
    .filter((finding) => finding.status !== "Resolved")
    .sort((a, b) => severityRank(b.severity) - severityRank(a.severity));
  const lead = findings[0];
  const loop = loopStates(data, coverage, linked);
  const name = business?.name ?? data.tenantName;
  const inspect = stage ?? CORE_LOOP.find((item) => loop.states[item] === "now") ?? CORE_LOOP[0];
  const inspectHref = STAGE_HREF[inspect];

  return (
    <div className="cc exec">
      <section className="cc-hero" aria-labelledby="cc-name">
        <Reveal>
          <p className="hero-kicker">{tenantTypeLabel(data.tenantType)} · {data.planName}</p>
          <h1 className="cc-name" id="cc-name"><Headline text={name} /></h1>
          <p className="cc-state">
            {data.lastScanAtUtc ? (
              <>
                <b>{data.openFindingCount} open signal{data.openFindingCount === 1 ? "" : "s"}</b> from the last DigitalPulse Check, {relativeTime(data.lastScanAtUtc)}.{" "}
              </>
            ) : (
              <>No DigitalPulse Check has run yet. </>
            )}
            <b>{linked} of {catalog.length || "—"}</b> platforms authorized. The identity record is <b>{coverage}%</b> complete.
          </p>
          <Stagger className="cc-figures">
            <Figure value={data.openFindingCount} label="Open signals" tone={data.highFindingCount ? "failing" : data.openFindingCount ? "warning" : undefined} />
            <Figure value={data.highFindingCount} label="High severity" tone={data.highFindingCount ? "failing" : undefined} />
            <Figure value={linked} label="Platforms authorized" />
            <Figure value={data.contentHoldCount + data.socialBlockedCount} label="Content on hold" tone={data.contentHoldCount + data.socialBlockedCount ? "warning" : undefined} />
          </Stagger>
        </Reveal>
        <Reveal delay={0.1}>
          <PulseMeter
            score={coverage}
            label="Identity coverage"
            caption="Canonical record completeness. Not a live SEO, review, or reputation index."
            facets={identity.data ? identityFacets(identity.data) : undefined}
          />
        </Reveal>
      </section>

      <Reveal as="section">
        <SectionTitle
          kicker="Ecosystem"
          title="Where the business is present"
          action={<Link className="text-link" to="/app/connections">Open connections</Link>}
        />
        {connections.isLoading ? (
          <PageState mode="loading" title="Mapping platforms" />
        ) : catalog.length === 0 ? (
          <p className="empty-line"><strong>No platform adapters</strong>The catalog is empty for this plan.</p>
        ) : (
          <PlatformEcosystem
            centerLabel={name}
            centerMeta={`${linked} linked`}
            nodes={catalog.map((platform) => {
              const link = links.find((item) => item.platformCode === platform.code);
              return {
                code: platform.code,
                name: platform.name,
                category: platform.category,
                state: linkState(link),
                meta: link?.lastHealthAtUtc ? `Checked ${relativeTime(link.lastHealthAtUtc)}` : null,
                detail: platform.summary
              };
            })}
            onSelect={(code) => navigate(`/app/connections?platform=${code}`)}
            hint="Open in Connection Center"
            caption="Development grants only. Live provider data is not shown until real OAuth is configured."
          />
        )}
      </Reveal>

      <Reveal as="section">
        <SectionTitle
          kicker="Signals"
          title="What DigitalPulse detected"
          action={<Link className="text-link" to="/app/findings">All signals</Link>}
        />
        {scans.isLoading ? (
          <PageState mode="loading" title="Loading signals" />
        ) : findings.length === 0 ? (
          <p className="empty-line">
            <strong>{data.lastScanAtUtc ? "No open signals" : "No check has run"}</strong>
            {data.lastScanAtUtc
              ? "The latest check produced no unresolved, evidence-backed issues."
              : "Run DigitalPulse Check to compare identity, website, and authorized platforms."}
          </p>
        ) : (
          <Stagger className="signal-list">
            {findings.slice(0, 6).map((finding, index) => (
              <StaggerItem key={finding.id}>
                <SignalRow
                  index={index + 1}
                  severity={finding.severity}
                  title={finding.title}
                  meta={`${finding.category} · ${finding.evidence.length} evidence`}
                  status={finding.status}
                  onSelect={() => navigate(`/app/findings?signal=${finding.id}`)}
                />
              </StaggerItem>
            ))}
          </Stagger>
        )}
      </Reveal>

      <Reveal as="section" className="glass-panel loop-panel">
        <SectionTitle kicker="Autonomous core loop" title="Where this business is in the loop" />
        <LoopTrack states={loop.states} notes={loop.notes} selected={inspect} onSelect={setStage} />
        <div className="loop-inspect" aria-live="polite">
          <span className={`status-pill is-${loop.states[inspect]}`}>{loop.states[inspect]}</span>
          <strong>{inspect}</strong>
          <p>{loop.notes[inspect] ?? STAGE_COPY[inspect]}</p>
          <Link className="text-link" to={inspectHref}>Open {inspect.toLowerCase()}</Link>
        </div>
      </Reveal>

      <section className="cc-bento">
        <TiltCard className="glass-panel">
          <SectionTitle kicker="Governance" title="Action approval cockpit" action={<Link className="text-link" to="/app/actions">Action center</Link>} />
          <div className="lanes">
            <article className="lane is-auto">
              <span className="status-pill is-live">Autonomous lane</span>
              <h3>Low-risk internal work</h3>
              <p>{data.actionOpenCount} open action{data.actionOpenCount === 1 ? "" : "s"} can run inside DigitalPulse. Full Auto never invents a provider write.</p>
              <small>{data.actionHeldCount === 0 ? "No external write is waiting." : `${data.actionHeldCount} still need a live adapter or sign-off.`}</small>
            </article>
            <article className="lane is-assist">
              <span className="status-pill is-hold">Assisted / high risk</span>
              <h3>Current vs proposed</h3>
              <p>
                {lead
                  ? `${lead.title} · ${lead.evidence.length} evidence source${lead.evidence.length === 1 ? "" : "s"}.`
                  : "No high-risk proposal is waiting. DigitalPulse will not invent a diff."}
              </p>
              <small>{data.socialBlockedCount + data.contentHoldCount} publish hold{data.socialBlockedCount + data.contentHoldCount === 1 ? "" : "s"} · evidence required before execute.</small>
            </article>
          </div>
        </TiltCard>
        <TiltCard className="glass-panel">
          <SectionTitle kicker="Evidence" title="Ground-truth brief" />
          <Brief lead={lead} identity={identity.data} onOpen={(to) => navigate(to)} />
        </TiltCard>
      </section>

      <section className="cc-bento">
        <TiltCard className="glass-panel">
          <SectionTitle kicker="Audit stream" title="Stored events" action={<Link className="text-link" to="/app/findings">Signals</Link>} />
          <ol className="audit-stream">
            {auditRows(data, findings).map((row) => (
              <li key={row.id}>
                <time>{row.when}</time>
                <code>{row.ctx}</code>
                <span>{row.detail}</span>
              </li>
            ))}
          </ol>
        </TiltCard>
        <TiltCard className="glass-panel">
          <SectionTitle kicker="Channels" title="Connectivity matrix" action={<Link className="text-link" to="/app/connections">Connections</Link>} />
          {catalog.length === 0 ? (
            <p className="empty-line"><strong>No platform adapters</strong>The catalog is empty for this plan.</p>
          ) : (
            <ul className="channel-matrix">
              {catalog.map((platform) => {
                const link = links.find((item) => item.platformCode === platform.code);
                const state = linkState(link);
                return (
                  <li key={platform.code} className={`is-${state}`}>
                    <button type="button" onClick={() => navigate(`/app/connections?platform=${platform.code}`)}>
                      <span className="channel-mark">
                        <BrandMark code={platform.code} name={platform.name} />
                      </span>
                      <strong>{platform.name}</strong>
                      <span className={`status-pill is-${state}`}>{state}</span>
                      <small>{link?.grantKind ?? "Not connected"}</small>
                      <small>{link?.lastHealthAtUtc ? `Checked ${relativeTime(link.lastHealthAtUtc)}` : "No health probe"}</small>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </TiltCard>
      </section>

      <section className="cc-ops">
        <Reveal>
          <SectionTitle kicker="Actions" title="In the queue" action={<Link className="text-link" to="/app/actions">Action center</Link>} />
          <Stagger className="watch" as="ul" gap={0.04}>
            <Watch label="Open work actions" value={String(data.actionOpenCount)} tone={data.actionOpenCount ? "warning" : "idle"} />
            <Watch label="Actions held for approval or a live write" value={String(data.actionHeldCount)} tone={data.actionHeldCount ? "warning" : "idle"} />
            <Watch label="Social drafts awaiting approval" value={String(data.socialDraftCount)} tone={data.socialDraftCount ? "warning" : "idle"} />
            <Watch label="Approved posts held from publishing" value={String(data.socialBlockedCount)} tone={data.socialBlockedCount ? "warning" : "idle"} />
            <Watch label="Project variants outside permission scope" value={String(data.contentHoldCount)} tone={data.contentHoldCount ? "warning" : "idle"} />
            <Watch label="Directory playbooks open" value={String(data.directoryOpenCount)} tone={data.directoryOpenCount ? "warning" : "idle"} />
            <Watch label="AI runs held for review" value={String(data.aiHeldCount)} tone={data.aiHeldCount ? "warning" : "idle"} />
            <Watch label="WhatsApp opt-ins" value={String(data.whatsAppOptInCount)} tone={data.whatsAppOptInCount ? "live" : "idle"} />
            <Watch label="WhatsApp messages held" value={String(data.whatsAppHeldCount)} tone={data.whatsAppHeldCount ? "warning" : "idle"} />
          </Stagger>
        </Reveal>
        <Reveal delay={0.08}>
          <SectionTitle kicker="Monitoring" title="What DigitalPulse is watching" action={<Link className="text-link" to="/app/monitoring">Monitoring workspace</Link>} />
          <Stagger className="watch" as="ul" gap={0.04}>
            <Watch label="DigitalPulse Check" value={relativeTime(data.lastScanAtUtc) ?? "Never run"} tone={data.lastScanAtUtc ? "live" : "idle"} />
            <Watch
              label="Website snapshot"
              value={data.lastWebsiteAtUtc ? `${data.websiteObservationCount} observations · ${relativeTime(data.lastWebsiteAtUtc)}` : "Not analyzed"}
              tone={data.lastWebsiteAtUtc ? "live" : "idle"}
            />
            <Watch label="Site search index" value={data.searchProvider} tone="live" />
            <Watch label="Directories verified" value={`${data.directoryVerifiedCount}`} tone={data.directoryVerifiedCount ? "live" : "idle"} />
            <Watch
              label="Continuous monitoring"
              value={data.lastMonitoringAtUtc ? `${data.openAlertCount} open alert${data.openAlertCount === 1 ? "" : "s"} · ${relativeTime(data.lastMonitoringAtUtc)}` : data.monitoringHoldReason}
              tone={data.openAlertCount ? "warning" : data.lastMonitoringAtUtc ? "live" : "idle"}
            />
            <Watch label="Presence reports" value={String(data.reportCount)} tone={data.reportCount ? "live" : "idle"} />
            <Watch label="Plan interval" value={`Every ${data.monitoringIntervalHours}h`} tone="idle" />
            <Watch
              label="Subscription"
              value={`${data.subscriptionStatus} · ${data.heldInvoiceCount} held invoice${data.heldInvoiceCount === 1 ? "" : "s"}`}
              tone={data.heldInvoiceCount ? "warning" : "live"}
            />
            {data.tenantType === "Agency" ? (
              <Watch
                label="Company clients"
                value={`${data.agencyClientCount} client business${data.agencyClientCount === 1 ? "" : "es"}`}
                tone={data.agencyClientCount ? "live" : "idle"}
              />
            ) : null}
            <Watch
              label="Operations"
              value={data.lastBackupAtUtc ? `${data.readinessHoldCount} readiness hold${data.readinessHoldCount === 1 ? "" : "s"}` : data.operationsHoldReason}
              tone={data.readinessHoldCount ? "warning" : data.lastBackupAtUtc ? "live" : "idle"}
            />
          </Stagger>
        </Reveal>
      </section>

      {data.businesses.length > 1 ? (
        <Reveal as="section">
          <SectionTitle kicker="Portfolio" title="Businesses in this workspace" />
          <DataGrid
            noun="business"
            empty="Create a business during onboarding."
            columns={["Business", "Website", "Locations"]}
            rows={data.businesses.map((item) => ({
              id: item.id,
              search: `${item.name} ${item.website ?? ""}`.toLowerCase(),
              cells: [item.name, item.website ?? "—", String(item.locationCount)],
              actions: <button type="button" className="grid-action" onClick={() => navigate(`/app/businesses/${item.id}`)}>Identity</button>
            }))}
            onRow={(id) => navigate(`/app/businesses/${id}`)}
          />
        </Reveal>
      ) : null}
    </div>
  );
}

const STAGE_HREF: Record<LoopStage, string> = {
  Connect: "/app/connections",
  Understand: "/app/website",
  Detect: "/app/findings",
  Create: "/app/social",
  Approve: "/app/actions",
  Execute: "/app/actions",
  Verify: "/app/directories",
  Monitor: "/app/monitoring"
};

const STAGE_COPY: Record<LoopStage, string> = {
  Connect: "Authorize official adapters. Development grants stay labeled as such.",
  Understand: "Identity and website snapshots are stored. Live Search Console waits for OAuth.",
  Detect: "DigitalPulse Check compares the identity record to stored evidence.",
  Create: "Drafts stay in DigitalPulse until an official write exists.",
  Approve: "High-risk work requires a human. Autopilot does not invent a publish.",
  Execute: "Internal checks can run. Live publishes stay held without a live credential.",
  Verify: "Directory verification is operator-confirmed. Unofficial writes are out of scope.",
  Monitor: "Stored health is recorded. Live likes, reviews, and spend are not invented."
};

function identityFacets(data: BusinessIdentity) {
  return [
    { label: "Website", ok: Boolean(data.business.website) },
    { label: "Contacts", ok: data.contacts.length > 0 },
    { label: "Category", ok: data.categories.length > 0 },
    { label: "Approved fact", ok: data.facts.some((fact) => fact.status === "Approved") }
  ];
}

function auditRows(data: DashboardResponse, findings: Finding[]) {
  const rows = [
    data.lastScanAtUtc
      ? { id: "scan", when: relativeTime(data.lastScanAtUtc) ?? "Stored", ctx: "check", detail: `${data.openFindingCount} open finding${data.openFindingCount === 1 ? "" : "s"} from DigitalPulse Check.` }
      : { id: "scan", when: "—", ctx: "check", detail: "No DigitalPulse Check has been stored." },
    data.lastWebsiteAtUtc
      ? { id: "web", when: relativeTime(data.lastWebsiteAtUtc) ?? "Stored", ctx: "website", detail: `${data.websiteObservationCount} on-page observations. Search provider ${data.searchProvider}.` }
      : { id: "web", when: "—", ctx: "website", detail: "Website has not been analyzed." },
    data.lastMonitoringAtUtc
      ? { id: "mon", when: relativeTime(data.lastMonitoringAtUtc) ?? "Stored", ctx: "monitor", detail: `${data.openAlertCount} open alert${data.openAlertCount === 1 ? "" : "s"}. Live provider metrics stay held.` }
      : { id: "mon", when: "—", ctx: "monitor", detail: data.monitoringHoldReason },
    { id: "bill", when: "now", ctx: "billing", detail: `${data.subscriptionStatus} · ${data.heldInvoiceCount} held invoice${data.heldInvoiceCount === 1 ? "" : "s"}. Captures are not invented.` }
  ];
  const lead = findings[0];
  if (lead) {
    rows.unshift({
      id: lead.id,
      when: "latest",
      ctx: lead.category.toLowerCase(),
      detail: `${lead.title} · ${lead.evidence.length} evidence source${lead.evidence.length === 1 ? "" : "s"}.`
    });
  }
  return rows.slice(0, 6);
}

function Headline({ text }: { text: string }) {
  const words = text.trim().split(/\s+/);
  if (words.length < 2) return <>{text}</>;
  return <>{words.slice(0, -1).join(" ")} <em>{words[words.length - 1]}</em></>;
}

function Figure({ value, label, tone }: { value: number; label: string; tone?: "failing" | "warning" }) {
  return (
    <StaggerItem className={tone ? `cc-figure is-${tone}` : "cc-figure"}>
      <strong className="num">{String(value).padStart(2, "0")}</strong>
      <span>{label}</span>
    </StaggerItem>
  );
}

function Watch({ label, value, tone }: { label: string; value: string; tone: "live" | "warning" | "failing" | "idle" }) {
  return (
    <StaggerItem as="li" className={`is-${tone}`}>
      <i aria-hidden="true" />
      <span>{label}</span>
      <small>{value}</small>
    </StaggerItem>
  );
}

function Brief({ lead, identity, onOpen }: { lead?: Finding; identity?: BusinessIdentity; onOpen: (to: string) => void }) {
  if (lead) {
    const rank = severityRank(lead.severity);
    const steps: BriefStep[] = [
      { label: "Observed", value: lead.title, tone: rank >= 3 ? "failing" : rank === 2 ? "warning" : undefined },
      { label: "Why it matters", value: lead.description },
      {
        label: "Evidence",
        value: lead.evidence.length
          ? `${lead.evidence.length} source${lead.evidence.length === 1 ? "" : "s"}: ${[...new Set(lead.evidence.map((item) => item.source))].join(", ")}`
          : "No evidence attached."
      },
      { label: "Recommendation", value: lead.recommendation },
      { label: "Action", value: lead.suggestedAction },
      { label: "Result", value: `${lead.status} · ${lead.automationState}`, tone: lead.status === "Resolved" ? "live" : "idle" }
    ];
    return (
      <AIBrief
        title={`${lead.severity} signal · ${lead.category}`}
        steps={steps}
        provenance="Assembled from DigitalPulse Check evidence. No language model generated this text."
        footer={<Button appearance="primary" onClick={() => onOpen(`/app/findings?signal=${lead.id}`)}>Investigate</Button>}
      />
    );
  }

  const gap = identity ? identityGap(identity) : null;
  if (gap) {
    return (
      <AIBrief
        title="Identity gap"
        steps={[
          { label: "Observed", value: gap.observed, tone: "warning" },
          { label: "Why it matters", value: gap.why },
          { label: "Recommendation", value: gap.action },
          { label: "Result", value: "Waiting on the identity record", tone: "idle" }
        ]}
        provenance="Derived from the canonical identity record. No language model generated this text."
        footer={identity ? <Button appearance="primary" onClick={() => onOpen(`/app/businesses/${identity.business.id}`)}>Open identity</Button> : null}
      />
    );
  }

  return (
    <AIBrief
      title="Nothing needs attention"
      steps={[
        { label: "Observed", value: "No open signals and no identity gaps.", tone: "live" },
        { label: "Recommendation", value: "Run DigitalPulse Check after the next change to the business." }
      ]}
      provenance="Derived from the identity record and the latest check."
    />
  );
}

function identityGap(data: BusinessIdentity) {
  if (!data.business.website) return { observed: "No official website on the record.", why: "Every listing comparison is anchored to the official site.", action: "Add the website on Profile." };
  if (!data.contacts.length) return { observed: "No phone or email recorded.", why: "Contact details are the source of truth for name, address, and phone checks.", action: "Add a contact point." };
  if (!data.categories.length) return { observed: "The business has no category.", why: "Directories place listings by category.", action: "Add a category." };
  if (!data.facts.some((fact) => fact.status === "Approved")) return { observed: "No approved facts.", why: "Draft and restricted claims can never be published.", action: "Approve one fact you can stand behind." };
  return null;
}

function loopStates(data: DashboardResponse, coverage: number, linked: number) {
  const done: Record<LoopStage, boolean | "held"> = {
    Connect: linked > 0,
    Understand: coverage >= 50 || Boolean(data.lastWebsiteAtUtc),
    Detect: Boolean(data.lastScanAtUtc),
    Create: data.socialDraftCount + data.socialBlockedCount + data.projectCount > 0,
    Approve: data.socialBlockedCount + data.contentHoldCount > 0,
    Execute: data.actionHeldCount > 0 ? "held" : data.actionOpenCount > 0,
    Verify: data.directoryVerifiedCount > 0,
    Monitor: data.lastMonitoringAtUtc ? true : data.openAlertCount > 0 ? "held" : false
  };
  const notes: Partial<Record<LoopStage, string>> = {
    Execute: data.actionHeldCount > 0
      ? "Some actions wait for approval or a live write"
      : "Internal checks can run. Live publishes stay held.",
    Monitor: data.lastMonitoringAtUtc
      ? "Stored health is recorded. Live provider metrics stay held."
      : "Run monitoring to record stored health. Live APIs are not invented."
  };
  let nowAssigned = false;
  const states = {} as Record<LoopStage, StageState>;
  for (const stage of CORE_LOOP) {
    const value = done[stage];
    if (value === "held") states[stage] = "held";
    else if (value) states[stage] = "done";
    else if (!nowAssigned) {
      states[stage] = "now";
      nowAssigned = true;
    } else states[stage] = "next";
  }
  return { states, notes };
}

function scoreIdentity(data: BusinessIdentity) {
  const checks = [
    Boolean(data.business.name.trim()),
    Boolean(data.business.website),
    Boolean(data.business.industryCode),
    Boolean(data.business.foundedYear),
    data.contacts.length > 0,
    data.categories.length > 0,
    data.services.length > 0,
    data.facts.some((fact) => fact.status === "Approved")
  ];
  return Math.round((checks.filter(Boolean).length / checks.length) * 100);
}
