import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ApiError, api, type BusinessResponse, type ContentHubWorkspace, type HubContent, type HubContentDraft, type HubContentSummary, type HubMediaAsset } from "../lib/api";
import { HubArticleView } from "../components/HubArticleView";
import { HubAssist } from "../components/HubAssist";
import { HubRichEditor } from "../components/HubRichEditor";
import { HubSeoHealth } from "../components/HubSeoHealth";
import { liveHubSeo } from "../lib/hubSeo";
import { PageState } from "../components/PageState";
import { Button } from "../design/Button";
import { DataGrid } from "../design/DataGrid";
import { AreaField, Field, SelectField } from "../design/Field";
import { useSession } from "../state/session";

const PANES = [
  ["overview", "Overview"],
  ["hub", "Hub"],
  ["ideas", "Ideas"],
  ["calendar", "Calendar"],
  ["drafts", "Drafts"],
  ["published", "Published"],
  ["seo", "SEO"],
  ["distribution", "Distribution"],
  ["analytics", "Analytics"]
] as const;

type Pane = (typeof PANES)[number][0];

const PIPELINE = ["Idea", "Draft", "Review", "Approved", "Scheduled", "Published", "Monitored"] as const;

function paneFrom(search: string | null): Pane {
  const value = search as Pane | null;
  return PANES.some(([id]) => id === value) ? (value as Pane) : "hub";
}

function emptyDraft(type = "ARTICLE"): HubContentDraft {
  return {
    contentTypeCode: type,
    title: "",
    excerpt: "",
    body: "",
    visibility: "Public",
    slug: "",
    focusKeyword: "",
    canonicalUrl: "",
    categories: [],
    tags: [],
    featuredMediaAssetId: null,
    metaTitle: "",
    metaDescription: ""
  };
}

function fromSelected(item: HubContent): HubContentDraft {
  return {
    contentTypeCode: item.contentTypeCode,
    title: item.title,
    excerpt: item.excerpt,
    body: item.body,
    visibility: item.visibility,
    slug: item.slug,
    focusKeyword: item.seo.focusKeyword ?? "",
    canonicalUrl: item.canonicalUrl ?? "",
    categories: item.categories,
    tags: item.tags,
    featuredMediaAssetId: item.featuredMediaAssetId,
    metaTitle: item.seo.metaTitle ?? "",
    metaDescription: item.seo.metaDescription ?? ""
  };
}

function tomorrowLocal() {
  const at = new Date(Date.now() + 24 * 60 * 60 * 1000);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${at.getFullYear()}-${pad(at.getMonth() + 1)}-${pad(at.getDate())}T${pad(at.getHours())}:${pad(at.getMinutes())}`;
}

function publicHref(businessId: string, item: { slug: string; status: string; visibility: string }) {
  return item.status === "Published" && item.visibility === "Public" ? `/hub/${businessId}/${item.slug}` : null;
}

function typeName(data: ContentHubWorkspace, code: string) {
  return data.types.find((type) => type.code === code)?.name ?? code;
}

function featuredUrl(draft: HubContentDraft, media: HubMediaAsset[], selected: HubContent | null) {
  const id = draft.featuredMediaAssetId;
  if (!id) return null;
  return media.find((item) => item.id === id)?.sourceUrl
    ?? selected?.media?.find((item) => item.mediaAssetId === id)?.sourceUrl
    ?? null;
}

function csv(values?: string[]) {
  return (values ?? []).map((value) => value.trim()).filter(Boolean);
}

function payload(draft: HubContentDraft) {
  return {
    ...draft,
    slug: draft.slug || null,
    focusKeyword: draft.focusKeyword || null,
    canonicalUrl: draft.canonicalUrl || null,
    metaTitle: draft.metaTitle || null,
    metaDescription: draft.metaDescription || null,
    categories: csv(draft.categories),
    tags: csv(draft.tags)
  };
}

const BUSINESS_KEY = "dp.content.businessId";

export function ContentPage() {
  const [params, setParams] = useSearchParams();
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const ids = businesses.data?.map((item) => item.id) ?? [];
  const requested = params.get("business") ?? (typeof sessionStorage === "undefined" ? null : sessionStorage.getItem(BUSINESS_KEY));
  const businessId = (requested && ids.includes(requested) ? requested : undefined) ?? businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["content-hub", businessId],
    queryFn: () => api.contentHub(businessId!),
    enabled: Boolean(businessId)
  });

  useEffect(() => {
    if (!businessId) return;
    sessionStorage.setItem(BUSINESS_KEY, businessId);
    if (params.get("business") !== businessId) {
      const next = new URLSearchParams(params);
      next.set("business", businessId);
      setParams(next, { replace: true });
    }
  }, [businessId]);

  if (businesses.isLoading) return <PageState mode="loading" title="Opening Content Hub" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId || !businesses.data) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before writing hub articles." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading Content Hub" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Content Hub unavailable" />;

  return (
    <ContentStudio
      key={businessId}
      businessId={businessId}
      businesses={businesses.data}
      data={query.data}
      onBusiness={(id) => {
        sessionStorage.setItem(BUSINESS_KEY, id);
        const next = new URLSearchParams(params);
        next.set("business", id);
        next.delete("pane");
        setParams(next);
      }}
    />
  );
}

function ContentStudio({
  businessId,
  businesses,
  data,
  onBusiness
}: {
  businessId: string;
  businesses: BusinessResponse[];
  data: ContentHubWorkspace;
  onBusiness: (id: string) => void;
}) {
  const [params, setParams] = useSearchParams();
  const pane = paneFrom(params.get("pane"));
  const queryClient = useQueryClient();
  const [selectedId, setSelectedId] = useState<string | null>(data.items[0]?.id ?? null);
  const [draft, setDraft] = useState<HubContentDraft>(emptyDraft(data.types[0]?.code));
  const [dirty, setDirty] = useState(false);
  const [scheduleAt, setScheduleAt] = useState(tomorrowLocal);
  const [keyword, setKeyword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const detail = useQuery({
    queryKey: ["hub-content", businessId, selectedId],
    queryFn: () => api.hubContent(businessId, selectedId!),
    enabled: Boolean(selectedId)
  });
  const selected = selectedId ? detail.data ?? null : null;

  useEffect(() => {
    if (!selected) return;
    if (dirty && selected.id === selectedId) return;
    setDraft(fromSelected(selected));
    setKeyword(selected.seo.focusKeyword ?? "");
    setDirty(false);
  }, [selected?.id, selected?.updatedAtUtc]);

  function openPane(next: Pane, id?: string) {
    if (id) setSelectedId(id);
    const nextParams: Record<string, string> = { business: businessId };
    if (next !== "hub") nextParams.pane = next;
    setParams(nextParams);
  }

  function change(patch: Partial<HubContentDraft>) {
    setDirty(true);
    setDraft((current) => ({ ...current, ...patch }));
  }

  async function refresh(nextId?: string | null) {
    await queryClient.invalidateQueries({ queryKey: ["content-hub"] });
    await queryClient.invalidateQueries({ queryKey: ["hub-content"] });
    if (nextId !== undefined) setSelectedId(nextId);
  }

  async function run(action: () => Promise<unknown>, ok: string) {
    setBusy(true);
    setError(null);
    try {
      const result = await action();
      setSuccess(ok);
      setDirty(false);
      const next = result && typeof result === "object" && "id" in result ? (result as { id?: string | null }).id : undefined;
      await refresh(next === null ? null : next ?? selectedId);
    } catch (err) {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The Content Hub action failed.");
    } finally {
      setBusy(false);
    }
  }

  function startNew() {
    setSelectedId(null);
    setDraft(emptyDraft(data.types[0]?.code));
    setKeyword("");
    setDirty(false);
    setSuccess(null);
    setError(null);
    openPane("hub");
  }

  async function save() {
    if (!draft.title.trim() || draft.body.trim().length < 8) {
      setError("Add a title and at least a short article body before saving.");
      return;
    }
    await run(
      () => selectedId
        ? api.updateHubContent(businessId, selectedId, { ...payload(draft), changeSummary: "Edited in the Content Hub" })
        : api.createHubContent(businessId, payload(draft)),
      selectedId ? "Revision stored." : "Draft stored on this business."
    );
  }

  async function schedule() {
    if (!selectedId) {
      setError("Save the article first, then schedule it.");
      return;
    }
    if (dirty) {
      setError("Save changes before scheduling.");
      return;
    }
    if (selected?.status !== "Approved" && selected?.status !== "Scheduled") {
      setError("Approve the draft before scheduling.");
      return;
    }
    const when = scheduleAt ? new Date(scheduleAt) : new Date(Date.now() + 24 * 60 * 60 * 1000);
    if (Number.isNaN(when.getTime()) || when.getTime() <= Date.now()) {
      setError("Pick a schedule time in the future.");
      return;
    }
    await run(() => api.scheduleHubContent(businessId, selectedId, when.toISOString()), "Scheduled on the Content Hub calendar.");
  }

  const drafts = data.items.filter((item) => ["Draft", "Hold", "PendingApproval", "Rejected"].includes(item.status));
  const published = data.items.filter((item) => item.status === "Published");
  const live = published.filter((item) => item.visibility === "Public");
  const due = data.calendar.filter((item) => item.status === "Scheduled" && new Date(item.scheduledAtUtc).getTime() <= Date.now());

  return (
    <section className="studio">
      <header className="studio-hero">
        <div>
          <p className="hero-kicker">Content Hub</p>
          <h1 className="page-title">Editorial desk</h1>
          <p className="page-lead">Write, preview, approve, then publish for the selected business. Live platform posts still need an official connection.</p>
        </div>
        <div className="studio-hero-actions">
          {businesses.length > 1 ? (
            <SelectField
              label="Business"
              value={businessId}
              onChange={onBusiness}
              options={businesses.map((item) => ({ value: item.id, label: item.name }))}
            />
          ) : <p className="ink-muted">{businesses[0]?.name}</p>}
          <Button appearance="primary" onClick={startNew}>New article</Button>
        </div>
      </header>
      {error ? <p className="note-err" role="alert">{error}</p> : null}
      {success ? <p className="note-ok">{success}</p> : null}

      <div className="studio-panes" role="tablist" aria-label="Content Hub">
        {PANES.map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={pane === id}
            className={pane === id ? "studio-tab is-on" : "studio-tab"}
            onClick={() => openPane(id)}
          >
            {label}
          </button>
        ))}
      </div>

      {pane === "overview" ? (
        <Overview
          data={data}
          drafts={drafts}
          published={published}
          live={live}
          businessId={businessId}
          onOpen={(id, next) => openPane(next, id)}
        />
      ) : null}

      {pane === "hub" || pane === "drafts" ? (
        <HubDesk
          businessId={businessId}
          businessName={businesses.find((item) => item.id === businessId)?.name ?? "Business"}
          data={data}
          items={pane === "drafts" ? drafts : data.items}
          selected={selected}
          selectedId={selectedId}
          draft={draft}
          busy={busy}
          onMediaRegistered={() => { void queryClient.invalidateQueries({ queryKey: ["content-hub"] }); }}
          onSelect={(id) => { setDirty(false); setSelectedId(id); }}
          onChange={change}
          onSave={() => void save()}
          onApprove={() => {
            if (dirty) { setError("Save changes before approving."); return; }
            if (selectedId) void run(() => api.approveHubContent(businessId, selectedId), "Approved on this desk.");
          }}
          onReject={() => {
            if (dirty) { setError("Save changes before rejecting."); return; }
            if (selectedId) void run(() => api.rejectHubContent(businessId, selectedId), "Rejected. Publish stays blocked until it is approved again.");
          }}
          onPublish={() => {
            if (dirty) { setError("Save changes before publishing."); return; }
            if (selected?.status !== "Approved" && selected?.status !== "Scheduled") {
              setError("Approve the draft before publishing.");
              return;
            }
            if (selectedId) void run(() => api.publishHubContent(businessId, selectedId), selected?.visibility === "Public" ? "Published on the public hub." : "Marked published internally. Switch visibility to Public to show it on the hub.");
          }}
          onRestore={(revisionId) => selectedId && void run(() => api.restoreHubRevision(businessId, selectedId, revisionId), "Restored that revision as a new draft.")}
          onCancelSchedule={() => selectedId && void run(() => api.cancelHubSchedule(businessId, selectedId), "Schedule cancelled. The article is still approved.")}
          onVariants={() => selectedId && void run(() => api.createHubVariants(businessId, selectedId), "Platform variants drafted. Writes stay on hold without a live grant.")}
          onDelete={() => selectedId && void run(async () => {
            await api.deleteHubContent(businessId, selectedId);
            setDraft(emptyDraft(data.types[0]?.code));
            setDirty(false);
            return { id: null };
          }, "Draft removed.")}
          scheduleAt={scheduleAt}
          onScheduleAt={setScheduleAt}
          onSchedule={() => void schedule()}
        />
      ) : null}

      {pane === "published" ? (
        <PublishedLibrary
          businessId={businessId}
          items={published}
          data={data}
          onOpen={(id) => openPane("hub", id)}
          onArchive={(id) => void run(() => api.archiveHubContent(businessId, id), "Taken off the public hub.")}
        />
      ) : null}

      {pane === "ideas" ? (
        <IdeasDesk
          data={data}
          busy={busy}
          onDiscover={() => void run(() => api.discoverContentOpportunities(businessId), "Ideas built from stored services, projects, and findings.")}
          onDraft={(topic, opportunityId) => void run(async () => {
            const item = await api.generateHubContent(businessId, topic, opportunityId);
            openPane("hub", item.id);
            return item;
          }, "Draft assembled from stored records.")}
        />
      ) : null}

      {pane === "calendar" ? (
        <CalendarDesk
          data={data}
          items={data.items}
          selectedId={selectedId}
          scheduleAt={scheduleAt}
          due={due.length}
          busy={busy}
          onSelect={setSelectedId}
          onScheduleAt={setScheduleAt}
          onSchedule={() => void schedule()}
          onRelease={() => void run(() => api.releaseHubCalendar(businessId), due.length ? "Due articles were published." : "Nothing was due.")}
        />
      ) : null}

      {pane === "seo" ? (
        <SeoDesk
          businessId={businessId}
          data={data}
          selected={selected}
          selectedId={selectedId}
          keyword={keyword}
          onKeyword={setKeyword}
          onSelect={(id) => { setDirty(false); setSelectedId(id); }}
          onAnalyze={() => selectedId && void run(() => api.analyzeHubSeo(businessId, selectedId, keyword || undefined), "Checklist refreshed from the stored article.")}
        />
      ) : null}

      {pane === "distribution" ? (
        <DistributionDesk
          businessId={businessId}
          data={data}
          selected={selected}
          selectedId={selectedId}
          onSelect={setSelectedId}
          onDistribute={(provider) => selectedId && void run(() => api.distributeHubContent(businessId, selectedId, provider), "Distribution recorded. A hold means the provider was not written.")}
        />
      ) : null}

      {pane === "analytics" ? <AnalyticsDesk data={data} /> : null}
    </section>
  );
}

function Overview({
  data,
  drafts,
  published,
  live,
  businessId,
  onOpen
}: {
  data: ContentHubWorkspace;
  drafts: HubContentSummary[];
  published: HubContentSummary[];
  live: HubContentSummary[];
  businessId: string;
  onOpen: (id: string, pane: Pane) => void;
}) {
  return (
    <div className="band band-2">
      <article className="panel">
        <h2>Desk queue</h2>
        <p>{data.items.length} articles · {drafts.length} drafts · {live.length} live on the public hub · {data.opportunities.length} ideas</p>
        <ol className="content-pipe">
          {PIPELINE.map((stage) => <li key={stage}>{stage}</li>)}
        </ol>
        <DataGrid
          noun="draft"
          empty="Start a new article or assemble one from stored records in Ideas."
          columns={["Title", "Status", "Next"]}
          rows={drafts.slice(0, 8).map((item) => ({
            id: item.id,
            search: item.title.toLowerCase(),
            cells: [item.title, item.status, item.status === "Approved" ? "Publish" : "Edit"],
            actions: <button type="button" className="grid-action" onClick={() => onOpen(item.id, "hub")}>Open</button>
          }))}
        />
      </article>
      <article className="panel">
        <h2>Published on the hub</h2>
        {live.length === 0 ? (
          <p className="ink-muted">Nothing is live yet. Save as Public, Approve, then Publish. Readers open /hub/{businessId} without signing in.</p>
        ) : (
          <ul className="hub-live">
            {live.map((item) => {
              const href = publicHref(businessId, item)!;
              return (
                <li key={item.id}>
                  <strong>{item.title}</strong>
                  <p>{item.excerpt || "No excerpt stored."}</p>
                  <Link className="text-link" to={href}>Open public page</Link>
                </li>
              );
            })}
          </ul>
        )}
        {published.length > live.length ? (
          <p className="ink-muted">{published.length - live.length} published internally with Private visibility. They are not on the public hub.</p>
        ) : null}
      </article>
    </div>
  );
}

function HubDesk({
  businessId,
  businessName,
  data,
  items,
  selected,
  selectedId,
  draft,
  busy,
  onSelect,
  onChange,
  onSave,
  onApprove,
  onReject,
  onPublish,
  onRestore,
  onCancelSchedule,
  onVariants,
  onDelete,
  onMediaRegistered,
  scheduleAt,
  onScheduleAt,
  onSchedule
}: {
  businessId: string;
  businessName: string;
  data: ContentHubWorkspace;
  items: HubContentSummary[];
  selected: HubContent | null;
  selectedId: string | null;
  draft: HubContentDraft;
  busy: boolean;
  onSelect: (id: string) => void;
  onChange: (patch: Partial<HubContentDraft>) => void;
  onSave: () => void;
  onApprove: () => void;
  onReject: () => void;
  onPublish: () => void;
  onRestore: (revisionId: string) => void;
  onCancelSchedule: () => void;
  onVariants: () => void;
  onDelete: () => void;
  onMediaRegistered: () => void;
  scheduleAt: string;
  onScheduleAt: (value: string) => void;
  onSchedule: () => void;
}) {
  const [mode, setMode] = useState<"write" | "preview">("write");
  const href = selected ? publicHref(selected.businessId, selected) : null;
  const canPublish = selected?.status === "Approved" || selected?.status === "Scheduled";
  const media = data.media ?? [];
  return (
    <div className="band band-2">
      <article className="panel">
        <h2>Articles</h2>
        <DataGrid
          noun="article"
          empty="No articles yet. Write one in the editor."
          columns={["Title", "Type", "Status", "SEO"]}
          selectedId={selectedId}
          onRow={onSelect}
          rows={items.map((item) => ({
            id: item.id,
            search: `${item.title} ${item.contentTypeCode} ${item.status}`.toLowerCase(),
            cells: [item.title, typeName(data, item.contentTypeCode), item.status, `${item.seoScore}`]
          }))}
        />
      </article>
      <article className="panel">
        <h2>{selected ? selected.title : "New article"}</h2>
        {selected ? <p className="ink-muted">{selected.sourceNote}</p> : <p className="ink-muted">Save a draft, Approve it, then Publish. Public articles appear at /hub/{businessId} without signing in.</p>}
        <AuthorLine />
        {selected ? <PipelineStatus status={selected.status} /> : null}
        <div className="studio-panes" role="tablist" aria-label="Article editor">
          <button type="button" role="tab" aria-selected={mode === "write"} className={mode === "write" ? "studio-tab is-on" : "studio-tab"} onClick={() => setMode("write")}>Write</button>
          <button type="button" role="tab" aria-selected={mode === "preview"} className={mode === "preview" ? "studio-tab is-on" : "studio-tab"} onClick={() => setMode("preview")}>Preview</button>
        </div>
        {mode === "preview" ? (
          <HubArticleView
            compact
            businessName={businessName}
            contentType={typeName(data, draft.contentTypeCode)}
            title={draft.title}
            excerpt={draft.excerpt}
            body={draft.body}
            featuredImageUrl={featuredUrl(draft, media, selected)}
          />
        ) : (
        <form className="id-form" onSubmit={(event) => { event.preventDefault(); onSave(); }}>
          <SelectField
            label="Type"
            value={draft.contentTypeCode}
            onChange={(contentTypeCode) => onChange({ contentTypeCode })}
            options={data.types.map((type) => ({ value: type.code, label: type.name }))}
          />
          <Field label="Title" value={draft.title} onChange={(title) => onChange({ title })} required />
          <Field label="Slug" value={draft.slug ?? ""} onChange={(slug) => onChange({ slug })} hint="Unique on this business. Leave blank to build it from the title." />
          <Field label="Excerpt" value={draft.excerpt} onChange={(excerpt) => onChange({ excerpt })} hint="If empty, the first lines of the body are stored." />
          <div className="dp-field">
            <span>Article</span>
            <HubRichEditor
              value={draft.body}
              onChange={(body) => onChange({ body })}
              media={media}
              articles={data.items.filter((item) => item.id !== selectedId)}
              businessId={businessId}
              onUpload={async (file) => {
                const asset = await api.uploadHubMedia(businessId, file);
                onMediaRegistered();
                return asset;
              }}
            />
          </div>
          <HubAssist
            businessId={businessId}
            contentId={selectedId}
            section={[draft.title, draft.body].filter(Boolean).join("\n\n")}
            onAccept={(item) => {
              if (item.target === "metaTitle") onChange({ metaTitle: item.suggestion });
              else if (item.target === "metaDescription") onChange({ metaDescription: item.suggestion });
              else if (item.target === "append") {
                const heading = ({ social: "Social post", linkedin: "LinkedIn", google: "Google", instagram: "Instagram", youtube: "YouTube" } as Record<string, string>)[item.action] ?? item.action;
                onChange({ body: `${draft.body}\n\n# ${heading}\n\n${item.suggestion}`.trim() });
              } else onChange({ body: item.suggestion });
            }}
          />
          <SelectField
            label="Visibility"
            value={draft.visibility}
            onChange={(visibility) => onChange({ visibility })}
            options={[{ value: "Public", label: "Public — readers can open /hub/…" }, { value: "Private", label: "Private — internal only" }]}
          />
          <FeaturedMediaField
            businessId={businessId}
            media={media}
            value={draft.featuredMediaAssetId}
            busy={busy}
            onChange={(featuredMediaAssetId) => onChange({ featuredMediaAssetId })}
            onRegistered={onMediaRegistered}
          />
          <Field label="Focus keyword" value={draft.focusKeyword ?? ""} onChange={(focusKeyword) => onChange({ focusKeyword })} />
          <Field label="Meta title" value={draft.metaTitle ?? ""} onChange={(metaTitle) => onChange({ metaTitle })} hint="Stored snippet title. 12–70 characters." />
          <Field label="Meta description" value={draft.metaDescription ?? ""} onChange={(metaDescription) => onChange({ metaDescription })} hint="Stored snippet. 40–160 characters." />
          <Field label="Canonical URL" value={draft.canonicalUrl ?? ""} onChange={(canonicalUrl) => onChange({ canonicalUrl })} />
          <SeoHealth draft={draft} entities={data.entities ?? []} />
          <ChipField
            label="Categories"
            values={draft.categories ?? []}
            suggestions={data.categories.map((item) => item.name)}
            onChange={(categories) => onChange({ categories })}
          />
          <ChipField
            label="Tags"
            values={draft.tags ?? []}
            suggestions={data.tags.map((item) => item.name)}
            onChange={(tags) => onChange({ tags })}
          />
          <div className="row-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{selected ? "Save changes" : "Save draft"}</Button>
          </div>
        </form>
        )}
        {selected ? (
          <div className="row-actions">
            <Button appearance="subtle" disabled={busy || selected.status === "Approved" || selected.status === "Published"} onClick={onApprove}>Approve</Button>
            <Button appearance="subtle" disabled={busy || selected.status === "Published" || selected.status === "Archived"} onClick={onReject}>Reject</Button>
            <label className="dp-field">
              <span>Schedule</span>
              <input type="datetime-local" value={scheduleAt} onChange={(event) => onScheduleAt(event.target.value)} />
            </label>
            <Button appearance="subtle" disabled={busy || !canPublish} onClick={onSchedule}>Schedule</Button>
            {selected.status === "Scheduled" ? <Button appearance="subtle" disabled={busy} onClick={onCancelSchedule}>Cancel schedule</Button> : null}
            <Button appearance="primary" disabled={busy || !canPublish} onClick={onPublish}>Publish</Button>
            <Button appearance="subtle" disabled={busy} onClick={onVariants}>Make variants</Button>
            {selected.status !== "Published" ? <Button appearance="subtle" disabled={busy} onClick={onDelete}>Delete</Button> : null}
            <Link className="text-link" to={`/hub/${businessId}`} target="_blank" rel="noreferrer">Public hub</Link>
            {href ? <Link className="text-link" to={href} target="_blank" rel="noreferrer">Open public page</Link> : null}
          </div>
        ) : null}
        {selected && selected.revisions.length > 0 ? (
          <div>
            <h3>Revisions</h3>
            <ul className="stack-list">
              {selected.revisions.map((revision) => (
                <li key={revision.id}>
                  <span>v{revision.versionNumber} · {revision.changeSummary}</span>
                  {selected.status !== "Published" && selected.status !== "Archived" ? (
                    <Button appearance="subtle" disabled={busy} onClick={() => onRestore(revision.id)}>Restore</Button>
                  ) : null}
                </li>
              ))}
            </ul>
          </div>
        ) : null}
        {selected?.seo ? <SeoCard seo={selected.seo} fallback={selected} entities={data.entities ?? []} /> : null}
      </article>
    </div>
  );
}

function AuthorLine() {
  const profile = useSession((state) => state.profile);
  return <p className="ink-muted">Author · {profile?.displayName || profile?.email || "Signed-in user"}</p>;
}

function ChipField({
  label,
  values,
  suggestions,
  onChange
}: {
  label: string;
  values: string[];
  suggestions: string[];
  onChange: (values: string[]) => void;
}) {
  const [draft, setDraft] = useState("");
  const clean = values.map((value) => value.trim()).filter(Boolean);
  const unused = suggestions.filter((item) => !clean.some((value) => value.toLowerCase() === item.toLowerCase()));

  function add(value: string) {
    const next = value.trim();
    if (!next || clean.some((item) => item.toLowerCase() === next.toLowerCase())) return;
    onChange([...clean, next]);
    setDraft("");
  }

  return (
    <div className="dp-field">
      <span>{label}</span>
      <div className="hub-chips">
        {clean.map((item) => (
          <button key={item} type="button" className="hub-chip is-on" onClick={() => onChange(clean.filter((value) => value !== item))}>
            {item} ×
          </button>
        ))}
        {unused.slice(0, 8).map((item) => (
          <button key={item} type="button" className="hub-chip" onClick={() => add(item)}>{item}</button>
        ))}
      </div>
      <Field label={`Add ${label.toLowerCase()}`} value={draft} onChange={setDraft} hint="Press add after each name. Stored on this business." />
      <Button appearance="subtle" type="button" disabled={draft.trim().length < 2} onClick={() => add(draft)}>Add</Button>
    </div>
  );
}

function FeaturedMediaField({
  businessId,
  media,
  value,
  busy,
  onChange,
  onRegistered
}: {
  businessId: string;
  media: HubMediaAsset[];
  value?: string | null;
  busy: boolean;
  onChange: (id: string | null) => void;
  onRegistered: () => void;
}) {
  const [url, setUrl] = useState("");
  const [label, setLabel] = useState("");
  const [note, setNote] = useState<string | null>(null);
  const selected = media.find((item) => item.id === value);

  async function register() {
    setNote(null);
    try {
      const asset = await api.registerHubMedia(businessId, {
        label: label.trim() || "Featured image",
        kind: "Image",
        sourceUrl: url.trim()
      });
      onChange(asset.id);
      setUrl("");
      setLabel("");
      setNote("Image registered on this business. Save the article to attach it.");
      onRegistered();
    } catch (err) {
      setNote(err instanceof ApiError ? err.title : "The image URL was rejected. Use an https public host.");
    }
  }

  return (
    <div className="hub-featured-pick">
      <SelectField
        label="Featured image"
        value={value ?? "none"}
        onChange={(id) => onChange(id === "none" ? null : id)}
        options={[{ value: "none", label: "None" }, ...media.map((item) => ({ value: item.id, label: item.label }))]}
      />
      {selected?.sourceUrl ? <img className="hub-featured is-thumb" src={selected.sourceUrl} alt="" /> : null}
      <Field
        label="Register https image"
        type="url"
        value={url}
        onChange={setUrl}
        hint="Public https URL only. Local and private hosts are rejected."
      />
      <Field label="Image label" value={label} onChange={setLabel} />
      <Button appearance="subtle" disabled={busy || url.trim().length < 12} onClick={() => void register()}>Register image</Button>
      {note ? <p className="ink-muted">{note}</p> : null}
      <label className="dp-field">
        <span>Upload featured image</span>
        <input
          type="file"
          accept="image/jpeg,image/png,image/webp,image/gif"
          onChange={(event) => {
            const file = event.target.files?.[0];
            if (!file) return;
            void api.uploadHubMedia(businessId, file).then((asset) => {
              onChange(asset.id);
              onRegistered();
              setNote("Image stored on this host. Save the article to attach it.");
            }).catch((err) => {
              setNote(err instanceof ApiError ? err.title : "The image could not be stored.");
            });
          }}
        />
      </label>
    </div>
  );
}

function SeoHealth({ draft, entities }: { draft: HubContentDraft; entities: string[] }) {
  const seo = liveHubSeo({ ...draft, entities });
  return (
    <HubSeoHealth
      score={seo.seoScore}
      intent={seo.searchIntent}
      checks={seo.checks}
      aeo={seo.aeoChecks}
      notes={seo.notes}
      entitiesMentioned={seo.entitiesMentioned}
      entitiesTotal={seo.entitiesTotal}
      live
    />
  );
}

function PublishedLibrary({
  businessId,
  items,
  data,
  onOpen,
  onArchive
}: {
  businessId: string;
  items: HubContentSummary[];
  data: ContentHubWorkspace;
  onOpen: (id: string) => void;
  onArchive: (id: string) => void;
}) {
  if (items.length === 0) {
    return (
      <article className="panel">
        <h2>Published</h2>
        <p className="ink-muted">No published articles yet. Approve a Public draft, then Publish. The public hub is /hub/{businessId}.</p>
      </article>
    );
  }

  return (
    <div className="hub-library">
      {items.map((item) => {
        const href = publicHref(businessId, item);
        return (
          <article key={item.id} className="panel hub-card">
            <p className="hero-kicker">{typeName(data, item.contentTypeCode)} · {item.visibility}</p>
            <h2>{item.title}</h2>
            <p>{item.excerpt || "No excerpt stored."}</p>
            <p className="ink-muted">{item.publishedAtUtc ? new Date(item.publishedAtUtc).toUTCString() : "Published time not stored."}</p>
            <div className="row-actions">
              <Link className="text-link" to={`/hub/${businessId}`} target="_blank" rel="noreferrer">Public hub</Link>
              {href ? <Link className="text-link" to={href} target="_blank" rel="noreferrer">Read public page</Link> : <span className="ink-muted">Private — not on the public hub.</span>}
              <Button appearance="subtle" onClick={() => onOpen(item.id)}>Edit</Button>
              <Button appearance="subtle" onClick={() => onArchive(item.id)}>Archive</Button>
            </div>
          </article>
        );
      })}
    </div>
  );
}

function IdeasDesk({
  data,
  busy,
  onDiscover,
  onDraft
}: {
  data: ContentHubWorkspace;
  busy: boolean;
  onDiscover: () => void;
  onDraft: (topic: string, opportunityId?: string) => void;
}) {
  const [prompt, setPrompt] = useState("");
  const [ideaId, setIdeaId] = useState<string | null>(data.opportunities[0]?.id ?? null);
  const idea = data.opportunities.find((item) => item.id === ideaId) ?? null;
  return (
    <div className="band band-2">
      <article className="panel">
        <h2>Opportunities</h2>
        <p className="ink-muted">These topics are built from services, projects, and findings already stored for this business. Scores are coverage of existing titles, not invented search volume.</p>
        <Button appearance="subtle" disabled={busy} onClick={onDiscover}>Discover from records</Button>
        <DataGrid
          noun="idea"
          empty="Add a service, project, or finding, then discover."
          columns={["Topic", "Source", "Coverage", "Gap"]}
          selectedId={ideaId}
          onRow={setIdeaId}
          rows={data.opportunities.map((item) => ({
            id: item.id,
            search: `${item.topic} ${item.sourceType}`.toLowerCase(),
            cells: [item.topic, item.sourceType, item.coverageScore ?? "—", item.opportunityScore ?? "—"],
            actions: <button type="button" className="grid-action" onClick={() => onDraft(item.topic, item.id)}>Write draft</button>
          }))}
        />
      </article>
      <article className="panel">
        {idea ? (
          <>
            <h2>{idea.topic}</h2>
            <p className="ink-muted">{idea.sourceType} · {idea.status}</p>
            <p>{idea.description || "No stored description."}</p>
            <p>{idea.reason || "No stored reason."}</p>
            <ul className="stack-list">
              <li>Coverage {idea.coverageScore ?? "—"}</li>
              <li>Opportunity {idea.opportunityScore ?? "—"}</li>
              <li>Relevance {idea.relevanceScore ?? "—"}</li>
              <li>Competition {idea.competitionScore ?? "—"}</li>
              <li>Priority {idea.priority ?? "—"}</li>
            </ul>
            <Button appearance="primary" disabled={busy} onClick={() => onDraft(idea.topic, idea.id)}>Write this draft</Button>
          </>
        ) : (
          <>
            <h2>Assemble from evidence</h2>
            <p className="ink-muted">Select an idea, or write a brief. Uses approved facts and stored identity. A live model is used only when that provider is connected.</p>
          </>
        )}
        <AreaField label="Brief" value={prompt} onChange={setPrompt} />
        <Button appearance="subtle" disabled={busy || prompt.trim().length < 4} onClick={() => onDraft(prompt)}>Assemble draft</Button>
      </article>
    </div>
  );
}

function CalendarDesk({
  data,
  items,
  selectedId,
  scheduleAt,
  due,
  busy,
  onSelect,
  onScheduleAt,
  onSchedule,
  onRelease
}: {
  data: ContentHubWorkspace;
  items: HubContentSummary[];
  selectedId: string | null;
  scheduleAt: string;
  due: number;
  busy: boolean;
  onSelect: (id: string) => void;
  onScheduleAt: (value: string) => void;
  onSchedule: () => void;
  onRelease: () => void;
}) {
  return (
    <div className="band band-2">
      <article className="panel">
        <h2>Schedule an article</h2>
        <p className="ink-muted">Pick an article, set a future time, then schedule. Release due items publishes anything whose time has already passed.</p>
        <SelectField
          label="Article"
          value={selectedId ?? ""}
          onChange={onSelect}
          options={items.filter((item) => item.status !== "Archived").map((item) => ({ value: item.id, label: `${item.title} · ${item.status}` }))}
        />
        <label className="dp-field">
          <span>When</span>
          <input type="datetime-local" value={scheduleAt} onChange={(event) => onScheduleAt(event.target.value)} />
        </label>
        <div className="row-actions">
          <Button appearance="primary" disabled={busy || !selectedId} onClick={onSchedule}>Schedule</Button>
          <Button appearance="subtle" disabled={busy} onClick={onRelease}>Release due ({due})</Button>
        </div>
      </article>
      <article className="panel">
        <h2>Calendar</h2>
        <DataGrid
          noun="slot"
          empty="Nothing scheduled. Use the form to place an article on the calendar."
          columns={["Article", "When", "Channel", "Status"]}
          selectedId={selectedId}
          onRow={onSelect}
          rows={data.calendar.map((item) => ({
            id: item.contentItemId,
            search: `${item.title} ${item.channel}`.toLowerCase(),
            cells: [item.title, new Date(item.scheduledAtUtc).toUTCString(), item.channel, item.status]
          }))}
        />
      </article>
    </div>
  );
}

function SeoDesk({
  businessId,
  data,
  selected,
  selectedId,
  keyword,
  onKeyword,
  onSelect,
  onAnalyze
}: {
  businessId: string;
  data: ContentHubWorkspace;
  selected: HubContent | null;
  selectedId: string | null;
  keyword: string;
  onKeyword: (value: string) => void;
  onSelect: (id: string) => void;
  onAnalyze: () => void;
}) {
  return (
    <div className="band band-2">
      <article className="panel">
        <h2>Articles</h2>
        <DataGrid
          noun="article"
          empty="Save an article first."
          columns={["Title", "Score"]}
          selectedId={selectedId}
          onRow={onSelect}
          rows={data.items.map((item) => ({
            id: item.id,
            search: item.title.toLowerCase(),
            cells: [item.title, `${item.seoScore}`]
          }))}
        />
      </article>
      <article className="panel">
        <h2>Checklist</h2>
        <p className="ink-muted">Score is checks passed ÷ checks run. DigitalPulse will not invent an 87 or a ranking.</p>
        {selected ? (
          <>
            <Field label="Focus keyword" value={keyword} onChange={onKeyword} />
            <Button appearance="primary" onClick={onAnalyze}>Run checklist</Button>
            <SeoCard seo={selected.seo} fallback={selected} entities={data.entities ?? []} />
            {publicHref(businessId, selected) ? (
              <p><Link className="text-link" to={publicHref(businessId, selected)!}>Public page</Link></p>
            ) : null}
          </>
        ) : <p className="ink-muted">Select an article to run the checklist.</p>}
      </article>
    </div>
  );
}

function DistributionDesk({
  businessId,
  data,
  selected,
  selectedId,
  onSelect,
  onDistribute
}: {
  businessId: string;
  data: ContentHubWorkspace;
  selected: HubContent | null;
  selectedId: string | null;
  onSelect: (id: string) => void;
  onDistribute: (provider: string) => void;
}) {
  const [provider, setProvider] = useState("HUB");
  return (
    <div className="band band-2">
      <article className="panel">
        <h2>Article</h2>
        <SelectField
          label="Article"
          value={selectedId ?? ""}
          onChange={onSelect}
          options={data.items.map((item) => ({ value: item.id, label: `${item.title} · ${item.status}` }))}
        />
      </article>
      <article className="panel">
        <h2>Distribution</h2>
        <p className="ink-muted">HUB writes the DigitalPulse public page at /hub/{businessId}. LinkedIn, Facebook, and Google stay assisted until that official login exists.</p>
        <p><Link className="text-link" to={`/hub/${businessId}`} target="_blank" rel="noreferrer">Open public hub</Link></p>
        <SelectField
          label="Provider"
          value={provider}
          onChange={setProvider}
          options={[
            { value: "HUB", label: "Content Hub public page" },
            { value: "LINKEDIN", label: "LinkedIn" },
            { value: "FACEBOOK", label: "Facebook" },
            { value: "GOOGLE", label: "Google" }
          ]}
        />
        <Button appearance="primary" disabled={!selectedId} onClick={() => onDistribute(provider)}>Distribute</Button>
        {selected ? (
          <>
            <DataGrid
              noun="attempt"
              empty="No distribution attempts yet."
              columns={["Provider", "Status", "Detail"]}
              rows={selected.distributions.map((item) => ({
                id: item.id,
                search: `${item.providerCode} ${item.status}`.toLowerCase(),
                cells: [item.providerCode, item.status, item.failureReason ?? item.publishedAtUtc ?? ""]
              }))}
            />
            <DataGrid
              noun="variant"
              empty="Make variants from the Hub editor."
              columns={["Kind", "Status", "Hold"]}
              rows={selected.variants.map((item) => ({
                id: item.id,
                search: `${item.kind} ${item.status}`.toLowerCase(),
                cells: [item.kind, item.status, item.publicationHold]
              }))}
            />
          </>
        ) : null}
      </article>
    </div>
  );
}

function AnalyticsDesk({ data }: { data: ContentHubWorkspace }) {
  return (
    <article className="panel">
      <h2>Analytics</h2>
      <p className="ink-muted">Rows appear only after publish or an official provider returns views. Empty views are a hold, not a zero invented for the chart.</p>
      <DataGrid
        noun="metric"
        empty="Publish an article first. Views stay blank until a provider returns them."
        columns={["Provider", "Date", "Views", "Detail"]}
        rows={data.metrics.map((item, index) => ({
          id: `${item.providerCode}-${item.metricDate}-${index}`,
          search: `${item.providerCode} ${item.detail}`.toLowerCase(),
          cells: [item.providerCode, item.metricDate, item.views ?? "—", item.detail]
        }))}
      />
    </article>
  );
}

function SeoCard({
  seo,
  fallback,
  entities
}: {
  seo: HubContent["seo"];
  fallback?: Pick<HubContent, "title" | "excerpt" | "body" | "slug" | "canonicalUrl">;
  entities: string[];
}) {
  const live = fallback
    ? liveHubSeo({
        title: fallback.title,
        excerpt: fallback.excerpt,
        body: fallback.body,
        slug: fallback.slug,
        canonicalUrl: fallback.canonicalUrl ?? seo.canonicalUrl,
        focusKeyword: seo.focusKeyword,
        metaTitle: seo.metaTitle,
        metaDescription: seo.metaDescription,
        entities
      })
    : null;
  const checks = seo.checks?.length ? seo.checks : live?.checks ?? [];
  const aeo = seo.aeoChecks?.length ? seo.aeoChecks : live?.aeoChecks ?? [];
  return (
    <HubSeoHealth
      score={seo.seoScore}
      intent={seo.searchIntent}
      checks={checks}
      aeo={aeo}
      notes={seo.notes}
      entitiesMentioned={seo.entitiesMentioned ?? live?.entitiesMentioned ?? 0}
      entitiesTotal={seo.entitiesTotal ?? live?.entitiesTotal ?? 0}
    />
  );
}

function PipelineStatus({ status }: { status: string }) {
  const index = status === "Published" ? 5
    : status === "Scheduled" ? 4
      : status === "Approved" ? 3
        : status === "PendingApproval" ? 2
          : 1;
  return (
    <ol className="content-pipe">
      {PIPELINE.map((stage, i) => (
        <li key={stage} className={i <= index ? "is-on" : undefined}>{stage}</li>
      ))}
    </ol>
  );
}
