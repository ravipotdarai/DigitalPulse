import { Button } from "../design/Button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ApiError, api, type SocialChannel, type SocialContent, type SocialPostReview, type SocialWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { BrandMark } from "../design/BrandMark";
import { ChannelCard, ChannelChip } from "../design/ChannelCard";
import { DataGrid } from "../design/DataGrid";
import { AreaField, Field } from "../design/Field";
import { reviewDraft, type LiveReview } from "../lib/postChecks";
import { LINK_LABEL, linkState } from "../design/platforms";

type Pane = "login" | "presence" | "compose";
type DraftFields = {
  title: string;
  location: string;
  description: string;
  image: string;
  video: string;
  imageId: string;
  videoId: string;
  imageName: string;
  videoName: string;
};

const EMPTY_DRAFT: DraftFields = {
  title: "",
  location: "",
  description: "",
  image: "",
  video: "",
  imageId: "",
  videoId: "",
  imageName: "",
  videoName: ""
};

const STUDIOS: Record<string, { code: string; name: string; line: string; compose: string; image: string; video: string }> = {
  google: {
    code: "GOOGLE",
    name: "Google",
    line: "Log in, approve, and DigitalPulse posts to Google Business Profile through the official API.",
    compose: "A short Google post. Location should match the official NAP.",
    image: "Photo to send with the post",
    video: "Optional short clip"
  },
  facebook: {
    code: "FACEBOOK",
    name: "Facebook",
    line: "Log in, approve, and DigitalPulse posts the caption, photo, or short video to the Facebook Page you authorized.",
    compose: "Title, place, and a caption people can scan on a phone.",
    image: "Feed photo",
    video: "Short video"
  },
  instagram: {
    code: "INSTAGRAM",
    name: "Instagram",
    line: "Log in, approve, and DigitalPulse publishes through the official Instagram API.",
    compose: "Write the caption first. A public image URL works best for Instagram.",
    image: "Square or portrait still",
    video: "Short reel"
  },
  linkedin: {
    code: "LINKEDIN",
    name: "LinkedIn",
    line: "Log in, approve, and DigitalPulse posts to the LinkedIn profile or page you authorized.",
    compose: "Lead with the point. Location is optional for local news.",
    image: "Still to attach",
    video: "Short native video"
  },
  youtube: {
    code: "YOUTUBE",
    name: "YouTube",
    line: "Log in, approve, and DigitalPulse uploads the small video through the official YouTube Data API.",
    compose: "Title, thumbnail, and the video DigitalPulse should upload.",
    image: "Thumbnail",
    video: "Small video file"
  }
};

const COMMON_CODES = ["GOOGLE", "FACEBOOK", "INSTAGRAM", "LINKEDIN", "YOUTUBE"] as const;

export function SocialPage() {
  const { platform: slug } = useParams();
  const studio = slug ? STUDIOS[slug] : undefined;
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["social", businessId],
    queryFn: () => api.social(businessId!),
    enabled: Boolean(businessId)
  });
  const connections = useQuery({
    queryKey: ["connections", businessId],
    queryFn: () => api.connections(businessId!),
    enabled: Boolean(businessId)
  });

  if (slug && !studio) {
    return <PageState mode="empty" title="Unknown channel" detail="Choose Google, Facebook, Instagram, LinkedIn, or YouTube." />;
  }
  if (businesses.isLoading) return <PageState mode="loading" title="Opening social studio" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before drafting social content." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading social channels" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Social studio unavailable" />;

  return (
    <SocialStudio
      businessId={businessId}
      data={query.data}
      studio={studio}
      connections={connections.data?.connections ?? []}
    />
  );
}

function SocialStudio({
  businessId,
  data,
  studio,
  connections
}: {
  businessId: string;
  data: SocialWorkspace;
  studio?: (typeof STUDIOS)[string];
  connections: Awaited<ReturnType<typeof api.connections>>["connections"];
}) {
  const channel = studio ? data.channels.find((item) => item.platformCode === studio.code) : undefined;
  const linked = connections.find((item) => item.platformCode === studio?.code);
  const analyticsLive = connections.some((item) => item.platformCode === "GOOGLE_ANALYTICS" && item.hasLiveCredential);
  const [pane, setPane] = useState<Pane>(() => (!studio || (linked && linkState(linked) === "live") ? "compose" : "login"));
  const items = studio ? data.items.filter((item) => item.platformCode === studio.code) : data.items;

  return (
    <section className="studio">
      <header className="studio-hero">
        <div>
          <p className="hero-kicker">{studio ? studio.name : "Every channel"}</p>
          <h1 className="page-title">Social platform</h1>
          <p className="page-lead">{studio ? studio.line : "Write once, tick the platforms, attach the photo or short video, then approve. DigitalPulse posts through the official APIs you logged in to."}</p>
        </div>
        {studio ? <BrandMark code={studio.code} name={studio.name} /> : <BrandMark code="SOCIAL" name="Common" />}
      </header>

      <div className="studio-panes" role="tablist" aria-label="Social platform">
        {([
          ["login", "Login"],
          ["presence", "Presence"],
          ["compose", "Compose"]
        ] as const).map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={pane === id}
            className={pane === id ? "studio-tab is-on" : "studio-tab"}
            onClick={() => setPane(id)}
          >
            {label}
          </button>
        ))}
      </div>

      {pane === "login" ? (
        studio ? (
          <LoginPane businessId={businessId} studio={studio} channel={channel} linked={linked} />
        ) : (
          <article className="studio-card">
            <p className="hero-kicker">Official login</p>
            <h2>Connection center</h2>
            <p className="ink-muted">Authorize each platform once. Common post uses those grants when you approve.</p>
            <Link className="text-link" to="/app/connections">Open connection center</Link>
          </article>
        )
      ) : null}
      {pane === "presence" ? (
        studio ? (
          <PresencePane studio={studio} channel={channel} linked={linked} note={data.note} />
        ) : (
          <div className="channel-grid">
            {data.channels.map((item) => (
              <ChannelCard
                key={item.platformCode}
                code={item.platformCode}
                name={item.platformName}
                category={item.category}
                rows={[
                  { label: "Connection", value: item.connectionStatus ?? "Not connected", tone: item.connectionStatus === "Connected" ? "ok" : "hold" },
                  { label: "Publish", value: item.canPublish ? "Official capability listed" : "Assisted only", tone: "hold" }
                ]}
              />
            ))}
          </div>
        )
      ) : null}
      {pane === "compose" ? (
        <ComposePane
          businessId={businessId}
          data={data}
          studio={studio}
          items={items}
          common={!studio}
          analyticsLive={analyticsLive}
        />
      ) : null}
    </section>
  );
}

function LoginPane({
  businessId,
  studio,
  channel,
  linked
}: {
  businessId: string;
  studio: (typeof STUDIOS)[string];
  channel?: SocialChannel;
  linked?: Awaited<ReturnType<typeof api.connections>>["connections"][number];
}) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const start = useMutation({
    mutationFn: () => api.startConnection(businessId, studio.code),
    onSuccess: async (result) => {
      setError(null);
      if (result.authorizationUrl) {
        window.location.assign(result.authorizationUrl);
        return;
      }
      await queryClient.invalidateQueries({ queryKey: ["connections"] });
      await queryClient.invalidateQueries({ queryKey: ["social"] });
    },
    onError: (err) => setError(err instanceof ApiError ? err.title : "Could not start login.")
  });
  const state = linkState(linked);

  return (
    <article className="studio-card">
      <p className="hero-kicker">Official login</p>
      <h2>Connect {studio.name}</h2>
      <p className="ink-muted">
        Login uses the official adapter for this platform. Development grants stay labelled as development. Live tokens are not invented.
      </p>
      <dl className="facts">
        <div><dt>Status</dt><dd>{LINK_LABEL[state]}</dd></div>
        <div><dt>Grant</dt><dd>{linked?.grantKind ?? channel?.grantKind ?? "None"}</dd></div>
        <div><dt>Account</dt><dd>{linked?.externalAccount ?? "—"}</dd></div>
      </dl>
      {error ? <p className="note-err" role="alert">{error}</p> : null}
      <div className="id-form-actions">
        <Button appearance="primary" disabled={start.isPending} onClick={() => start.mutate()}>
          {start.isPending ? "Opening…" : linked ? "Reauthorize" : `Log in with ${studio.name}`}
        </Button>
        <Link className="text-link" to={`/app/connections?platform=${studio.code}`}>Open connection center</Link>
      </div>
    </article>
  );
}

function PresencePane({
  studio,
  channel,
  linked,
  note
}: {
  studio: (typeof STUDIOS)[string];
  channel?: SocialChannel;
  linked?: Awaited<ReturnType<typeof api.connections>>["connections"][number];
  note: string;
}) {
  const state = linkState(linked);
  return (
    <div className="studio-presence">
      <ChannelCard
        code={studio.code}
        name={studio.name}
        category={channel?.category ?? "Social"}
        rows={[
          { label: "Login", value: LINK_LABEL[state], tone: state === "live" ? "ok" : "hold" },
          { label: "Publish", value: channel?.canPublish ? "Official capability listed" : "Assisted only", tone: "hold" },
          { label: "Metrics", value: channel?.metricStatus ?? "Not captured", tone: channel?.metricStatus === "Hold" ? "warn" : "hold" },
          { label: "Grant", value: linked?.grantKind ?? channel?.grantKind ?? "None", tone: "hold" }
        ]}
      >
        {channel?.metricDetail ? <p className="ink-muted meta-line">{channel.metricDetail}</p> : null}
      </ChannelCard>
      <article className="studio-card">
        <p className="hero-kicker">Presence</p>
        <h2>What DigitalPulse knows about {studio.name}</h2>
        <p className="ink-muted">{note}</p>
        <p className="ink-muted">
          Presence is the authorized record for this platform — login, grant, and publish capability. Follower counts and reach are not invented.
        </p>
        {linked?.lastError ? <p className="note-err">{linked.lastError}</p> : null}
      </article>
    </div>
  );
}

function ComposePane({
  businessId,
  data,
  studio,
  items,
  common,
  analyticsLive
}: {
  businessId: string;
  data: SocialWorkspace;
  studio?: (typeof STUDIOS)[string];
  items: SocialContent[];
  common: boolean;
  analyticsLive: boolean;
}) {
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<DraftFields>(EMPTY_DRAFT);
  const [picked, setPicked] = useState<string[]>(studio ? [studio.code] : [...COMMON_CODES]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(items[0]?.id ?? null);
  const selected = items.find((item) => item.id === selectedId) ?? items[0] ?? null;
  const hint = studio ?? STUDIOS.facebook;

  useEffect(() => {
    setPicked(studio ? [studio.code] : [...COMMON_CODES]);
    setSelectedId(items[0]?.id ?? null);
  }, [studio?.code]);

  async function attachFile(file: File | undefined, kind: "image" | "video") {
    if (!file) return;
    try {
      const saved = await api.uploadSocialMedia(businessId, file);
      setError(null);
      setDraft((current) =>
        kind === "image"
          ? { ...current, imageId: saved.id, imageName: saved.fileName }
          : { ...current, videoId: saved.id, videoName: saved.fileName }
      );
      setSuccess(`${saved.fileName} is attached. Approve, then DigitalPulse will send it through the official API.`);
    } catch (err) {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "The file could not be stored.");
    }
  }

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["social"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  }

  const save = useMutation({
    mutationFn: async () => {
      const preview = reviewDraft(draft.title, draft.description, draft.location, Boolean(draft.imageId || draft.image.trim()), analyticsLive);
      if (preview.safetyStatus === "Banned") throw new ApiError(400, preview.safetyDetail);
      const body = packDraft(draft);
      const platforms = common ? picked : [studio!.code];
      if (platforms.length === 0) throw new ApiError(400, "Tick at least one platform.");
      const created: SocialContent[] = [];
      for (const platformCode of platforms) {
        created.push(await api.createSocial(businessId, { platformCode, title: draft.title, body }));
      }
      return created;
    },
    onSuccess: async (created) => {
      setError(null);
      setSuccess(created.length > 1
        ? `${created.length} drafts stored. Nothing was posted live.`
        : "Draft stored. It has not been posted to any live network.");
      setDraft(EMPTY_DRAFT);
      setSelectedId(created[0]?.id ?? null);
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

  const channels = useMemo(
    () => data.channels.filter((channel) => COMMON_CODES.includes(channel.platformCode as (typeof COMMON_CODES)[number])),
    [data.channels]
  );
  const live = reviewDraft(
    draft.title,
    draft.description,
    draft.location,
    Boolean(draft.imageId || draft.image.trim()),
    analyticsLive
  );

  return (
    <div className="studio-compose">
      <aside className="studio-card studio-form draft-form">
        <p className="hero-kicker">{common ? "Shared draft" : hint.name}</p>
        <h2>{common ? "Post to selected platforms" : "New draft"}</h2>
        {common ? (
          <div className="draft-channels" role="group" aria-label="Platforms">
            {channels.map((channel) => (
              <ChannelChip
                key={channel.platformCode}
                code={channel.platformCode}
                name={channel.platformName}
                selected={picked.includes(channel.platformCode)}
                onSelect={() => {
                  setPicked((current) => current.includes(channel.platformCode)
                    ? current.filter((code) => code !== channel.platformCode)
                    : [...current, channel.platformCode]);
                }}
              />
            ))}
          </div>
        ) : null}
        <Field label="Title" value={draft.title} onChange={(value) => setDraft((d) => ({ ...d, title: value }))} required />
        <Field label="Location" value={draft.location} onChange={(value) => setDraft((d) => ({ ...d, location: value }))} hint="Used for local SEO." />
        <AreaField label="Description" value={draft.description} onChange={(value) => setDraft((d) => ({ ...d, description: value }))} />
        <div className="studio-media">
          <label className="dp-field">
            <span>Image</span>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,image/gif"
              aria-label="Image file"
              onChange={(event) => void attachFile(event.target.files?.[0], "image")}
            />
            <small className="ink-muted">{draft.imageName || hint.image}</small>
          </label>
          <label className="dp-field">
            <span>Video</span>
            <input
              type="file"
              accept="video/mp4,video/quicktime,video/webm"
              aria-label="Video file"
              onChange={(event) => void attachFile(event.target.files?.[0], "video")}
            />
            <small className="ink-muted">{draft.videoName || hint.video}</small>
          </label>
        </div>
        <Field label="Image URL" value={draft.image} onChange={(value) => setDraft((d) => ({ ...d, image: value }))} hint="Optional public URL, or attach a file above." />
        <Field label="Video URL" value={draft.video} onChange={(value) => setDraft((d) => ({ ...d, video: value }))} hint="Optional public URL, or attach a file above." />
        <ReviewCard review={live} />
        <Button appearance="primary" disabled={save.isPending || live.safetyStatus === "Banned"} onClick={() => save.mutate()}>
          {save.isPending ? "Saving…" : "Save draft"}
        </Button>
      </aside>

      <div className="studio-queue">
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
        {items.length === 0 ? (
          <div className="dp-empty dp-surface dp-empty-lg">
            <strong>No drafts yet</strong>
            <p>Save a draft, approve it, then publish. Delete removes unpublished drafts only.</p>
          </div>
        ) : (
          <DataGrid
            noun="draft"
            empty="Save a draft to start the approve → publish loop."
            selectedId={selected?.id}
            onRow={setSelectedId}
            columns={common ? ["Channel", "Title", "Status"] : ["Title", "Status", "Verify"]}
            rows={items.map((item) => ({
              id: item.id,
              search: `${item.platformCode} ${item.title} ${item.status}`.toLowerCase(),
              cells: common
                ? [item.platformCode, item.title, item.status]
                : [item.title, item.status, item.verificationStatus]
            }))}
          />
        )}
        {selected ? (
          <article className="studio-card">
            <p className="hero-kicker">{selected.kind} · {selected.status}</p>
            <h2>{selected.title}</h2>
            <p className="ink-muted studio-body">{selected.body}</p>
            {selected.review ? <ReviewCard review={selected.review} /> : null}
            {selected.verificationDetail ? <p>{selected.verificationDetail}</p> : null}
            {selected.lastPublishError ? <p className="note-err">{selected.lastPublishError}</p> : null}
            <div className="id-form-actions">
              {selected.status !== "Approved" && selected.status !== "Published" ? (
                <Button appearance="subtle" onClick={() => void run(() => api.approveSocial(businessId, selected.id), "Draft approved. Publish will send this through the official API you authorized.")}>
                  Approve
                </Button>
              ) : null}
              {selected.status === "Approved" ? (
                <Button appearance="primary" onClick={() => void run(() => api.publishSocial(businessId, selected.id), "Publish used the official API. A hold means the grant or the provider rejected the write — nothing was invented.")}>
                  Publish
                </Button>
              ) : null}
              {selected.status !== "Published" ? (
                <Button
                  appearance="subtle"
                  onClick={() => void run(async () => {
                    await api.deleteSocial(businessId, selected.id);
                    setSelectedId(null);
                  }, "Draft deleted.")}
                >
                  Delete
                </Button>
              ) : null}
            </div>
          </article>
        ) : null}
      </div>
    </div>
  );
}

function ReviewCard({ review }: { review: LiveReview | SocialPostReview }) {
  return (
    <div className="studio-review" aria-live="polite">
      <div>
        <span className={review.safetyStatus === "Banned" ? "sev sev-crit" : "sev sev-ok"}>{review.safetyStatus}</span>
        <strong>Safety</strong>
        <p>{review.safetyDetail}</p>
      </div>
      <div>
        <span className={review.seoStatus === "Ready" ? "sev sev-ok" : "sev sev-warn"}>{review.seoStatus}</span>
        <strong>SEO</strong>
        {review.seoNotes.length === 0 ? <p>Title, description, location, and image look ready for search.</p> : (
          <ul>{review.seoNotes.map((note) => <li key={note}>{note}</li>)}</ul>
        )}
      </div>
      <div>
        <span className={review.analyticsStatus === "Supported" ? "sev sev-ok" : "sev sev-hold"}>{review.analyticsStatus}</span>
        <strong>Google Analytics</strong>
        <p>{review.analyticsDetail}</p>
      </div>
    </div>
  );
}

function packDraft(draft: DraftFields) {
  const lines = [draft.description.trim()];
  if (draft.location.trim()) lines.unshift(`Location: ${draft.location.trim()}`);
  if (draft.imageId) lines.push(`ImageFile: ${draft.imageId}`);
  if (draft.videoId) lines.push(`VideoFile: ${draft.videoId}`);
  if (draft.image.trim()) lines.push(`Image: ${draft.image.trim()}`);
  if (draft.video.trim()) lines.push(`Video: ${draft.video.trim()}`);
  const body = lines.filter(Boolean).join("\n\n");
  if (!draft.title.trim()) throw new ApiError(400, "Add a title.");
  if (!body.trim()) throw new ApiError(400, "Add a description, or attach an image or video.");
  return body;
}
