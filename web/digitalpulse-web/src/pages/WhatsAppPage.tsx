import { Button } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type WhatsAppWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";
import { Field } from "../design/Field";

export function WhatsAppPage() {
  const businesses = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const businessId = businesses.data?.[0]?.id;
  const query = useQuery({
    queryKey: ["whatsapp", businessId],
    queryFn: () => api.whatsapp(businessId!),
    enabled: Boolean(businessId)
  });

  if (businesses.isLoading) return <PageState mode="loading" title="Opening WhatsApp" />;
  if (businesses.isError) return <PageState mode="error" title="Businesses unavailable" />;
  if (!businessId) return <PageState mode="empty" title="No business yet" detail="Finish onboarding before connecting Cloud API." />;
  if (query.isLoading) return <PageState mode="loading" title="Loading WhatsApp workspace" />;
  if (query.isError || !query.data) return <PageState mode="error" title="WhatsApp workspace unavailable" />;

  return <WhatsAppWorkspaceView businessId={businessId} data={query.data} />;
}

function WhatsAppWorkspaceView({ businessId, data }: { businessId: string; data: WhatsAppWorkspace }) {
  const queryClient = useQueryClient();
  const [displayName, setDisplayName] = useState(data.account?.displayName ?? "");
  const [phone, setPhone] = useState(data.account?.phoneNumber ?? "");
  const [templateName, setTemplateName] = useState("");
  const [templateBody, setTemplateBody] = useState("");
  const [campaignName, setCampaignName] = useState("");
  const [messageBody, setMessageBody] = useState("");
  const [inboundBody, setInboundBody] = useState("");
  const [contactId, setContactId] = useState(data.contacts[0]?.id ?? "");
  const [templateId, setTemplateId] = useState(data.templates[0]?.id ?? "");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(data.messages[0]?.id ?? null);
  const selected = data.messages.find((item) => item.id === selectedId) ?? data.messages[0] ?? null;

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["whatsapp"] });
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
      setError(err instanceof ApiError ? err.title : "WhatsApp could not continue.");
    }
  }

  const connect = useMutation({
    mutationFn: () => api.connectWhatsApp(businessId, { displayName, phoneNumber: phone }),
    onSuccess: async () => {
      setError(null);
      setSuccess("WhatsApp Business connected with a development grant. Phone verification still waits for Cloud API.");
      await refresh();
    },
    onError: (err) => {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "WhatsApp could not connect.");
    }
  });

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">WhatsApp Business Messaging</p>
        <h1 className="display display-page command-title">Cloud API workspace</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          {data.planEnabled ? "Plan includes WhatsApp" : "Plan does not include WhatsApp"} · {data.messagesUsedThisMonth} of {data.messagesPerMonth} messages this month
        </p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Business account</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              connect.mutate();
            }}
          >
            <Field label="Display name" value={displayName} onChange={setDisplayName} required />
            <Field label="Business phone" value={phone} onChange={setPhone} required />
            <Button appearance="primary" type="submit" disabled={connect.isPending || !data.planEnabled}>
              {connect.isPending ? "Connecting…" : "Connect Cloud API"}
            </Button>
          </form>
          {data.account ? (
            <>
              <div className="row-line"><span>Status</span><span className="sev sev-hold">{data.account.status}</span></div>
              <div className="row-line"><span>Phone</span><span>{data.account.phoneStatus}</span></div>
              <div className="row-line"><span>Provider</span><span>{data.account.cloudApiName}{data.account.cloudApiIsLive ? "" : " · not live"}</span></div>
              <p className="ink-muted">{data.account.holdReason}</p>
              <Button appearance="subtle" onClick={() => void run(() => api.verifyWhatsAppPhone(businessId), "Phone verification asked Cloud API. A verified number was not invented.")}>
                Verify phone
              </Button>
            </>
          ) : (
            <PageState mode="empty" title="No WhatsApp account" detail="Connect through official Cloud API. Unofficial clients are out of scope." />
          )}
        </article>
        <article className="panel">
          <h2>Analytics</h2>
          <div className="row-line"><span>Opted in</span><span>{data.analytics.optedIn}</span></div>
          <div className="row-line"><span>Opted out</span><span>{data.analytics.optedOut}</span></div>
          <div className="row-line"><span>Approved templates</span><span>{data.analytics.templatesApproved}</span></div>
          <div className="row-line"><span>Held messages</span><span>{data.analytics.messagesHeld}</span></div>
          <p className="ink-muted">{data.analytics.deliveryNote}</p>
        </article>
      </div>

      <div className="band band-2">
        <article className="panel">
          <h2>Audience</h2>
          <div className="id-form-actions">
            <Button appearance="subtle" onClick={() => void run(() => api.importWhatsAppContacts(businessId), "Authorized customer mobiles imported. They are not sendable until they opt in.")}>
              Import customers
            </Button>
          </div>
          <DataGrid
            noun="contact"
            empty="Import customers with a mobile number, then record an explicit opt-in."
            columns={["Name", "Mobile", "Consent", "Window"]}
            rows={data.contacts.map((item) => ({
              id: item.id,
              search: `${item.displayName} ${item.mobile} ${item.consent}`.toLowerCase(),
              cells: [item.displayName, item.mobile, item.consent, item.windowOpen ? "Open" : "Closed"],
              actions: (
                <>
                  <button type="button" className="grid-action" onClick={() => { setContactId(item.id); void run(() => api.optInWhatsApp(businessId, item.id), "Opt-in recorded."); }}>Opt in</button>
                  <button type="button" className="grid-action" onClick={() => void run(() => api.optOutWhatsApp(businessId, item.id), "Opt-out recorded. Campaign and template sends stop.")}>Opt out</button>
                </>
              )
            }))}
            onRow={(id) => setContactId(id)}
          />
        </article>
        <article className="panel">
          <h2>Templates and campaigns</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              void run(() => api.createWhatsAppTemplate(businessId, { name: templateName, language: "en", category: "UTILITY", body: templateBody }), "Template stored as an internal draft.");
            }}
          >
            <Field label="Template name" value={templateName} onChange={setTemplateName} required />
            <Field label="Template body" value={templateBody} onChange={setTemplateBody} required />
            <Button appearance="primary" type="submit" disabled={templateName.trim().length < 2 || templateBody.trim().length < 3}>Save template</Button>
          </form>
          {data.templates.map((item) => (
            <div className="row-line" key={item.id}>
              <span>{item.name} · {item.status}</span>
              <button type="button" className="grid-action" onClick={() => { setTemplateId(item.id); void run(() => api.approveWhatsAppTemplate(businessId, item.id), "Internally approved. Meta approval is not invented."); }}>Approve</button>
            </div>
          ))}
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              void run(() => api.createWhatsAppCampaign(businessId, { name: campaignName, templateId }), "Campaign drafted from the approved template.");
            }}
          >
            <Field label="Campaign name" value={campaignName} onChange={setCampaignName} required />
            <Button appearance="subtle" type="submit" disabled={!templateId || campaignName.trim().length < 3}>Create campaign</Button>
          </form>
          {data.campaigns.map((item) => (
            <div className="row-line" key={item.id}>
              <span>{item.name} · {item.status}</span>
              <span>
                <button type="button" className="grid-action" onClick={() => void run(() => api.approveWhatsAppCampaign(businessId, item.id), "Campaign approved. Sends still need opt-in and Cloud API.")}>Approve</button>
                <button type="button" className="grid-action" onClick={() => void run(() => api.scheduleWhatsAppCampaign(businessId, item.id, new Date().toISOString()), "Campaign evaluated. Live delivery was not invented.")}>Schedule</button>
              </span>
            </div>
          ))}
        </article>
      </div>

      <article className="panel">
        <h2>Messages</h2>
        <form
          className="id-form"
          onSubmit={(event) => {
            event.preventDefault();
            void run(() => api.draftWhatsApp(businessId, { contactId, kind: "Template", body: messageBody, templateId: templateId || null }), "Draft stored. It has not been sent.");
          }}
        >
          <Field label="Message" value={messageBody} onChange={setMessageBody} required />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={!contactId || messageBody.trim().length < 3}>Draft template</Button>
            <Button appearance="subtle" onClick={() => void run(() => api.draftWhatsAppAi(businessId, { contactId, kind: "Template", prompt: messageBody || "Write a short utility update.", templateId: templateId || null }), "AI draft stored. It was not sent.")}>AI draft</Button>
            <Button appearance="subtle" onClick={() => void run(() => api.recordWhatsAppInbound(businessId, { contactId, body: inboundBody || "Customer reply recorded by the operator." }), "Inbound recorded as untrusted. The 24-hour window opened.")}>Record inbound</Button>
          </div>
        </form>
        <Field label="Inbound note" value={inboundBody} onChange={setInboundBody} />
      </article>

      <DataGrid
        noun="message"
        empty="Draft a template or record an inbound reply. Cloud API sends stay held without a live token."
        columns={["Kind", "Status", "Hold"]}
        selectedId={selected?.id}
        rows={data.messages.map((item) => ({
          id: item.id,
          search: `${item.kind} ${item.status} ${item.body}`.toLowerCase(),
          cells: [item.kind, item.status, item.holdReason],
          actions: <button type="button" className="grid-action" onClick={() => setSelectedId(item.id)}>Open</button>
        }))}
        onRow={(id) => setSelectedId(id)}
      />

      {selected ? (
        <article className="panel">
          <p className="hero-kicker">{selected.kind}</p>
          <h2>{selected.status}</h2>
          <p className="ink-muted">{selected.body}</p>
          <p className="ink-muted">{selected.holdReason}</p>
          <div className="id-form-actions">
            {selected.status === "Draft" || selected.status === "Held" ? (
              <Button appearance="primary" onClick={() => void run(() => api.approveWhatsAppMessage(businessId, selected.id), "Approved. Policy still applies at send.")}>Approve</Button>
            ) : null}
            {selected.status === "Approved" || selected.status === "Held" || selected.status === "Failed" ? (
              <Button appearance="subtle" onClick={() => void run(() => api.sendWhatsAppMessage(businessId, selected.id), "Send asked Cloud API. A delivery was not invented.")}>Send</Button>
            ) : null}
          </div>
        </article>
      ) : (
        <PageState mode="empty" title="No message selected" detail="Draft, approve, then send. The engine blocks missing opt-in, templates, or a closed window." />
      )}
    </section>
  );
}
