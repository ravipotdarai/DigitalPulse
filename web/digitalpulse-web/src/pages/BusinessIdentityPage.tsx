import { Button } from "@fluentui/react-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, motion } from "framer-motion";
import { FormEvent, useState, type ReactNode } from "react";
import { useParams } from "react-router-dom";
import { ApiError, api, type BusinessIdentity } from "../lib/api";
import { PageState } from "../components/PageState";
import { AreaField, Field, Note, SelectField } from "../design/Field";
import { DataGrid, GridAction } from "../design/DataGrid";

const SECTIONS = [
  { id: "contacts", label: "Contacts", hint: "Phone, email, website, and listing URLs." },
  { id: "categories", label: "Categories", hint: "How directories should classify this business." },
  { id: "services", label: "Services", hint: "What the business sells or delivers." },
  { id: "brands", label: "Brands", hint: "Public names besides the legal name." },
  { id: "facts", label: "Facts", hint: "Approved claims can publish. Restricted claims stay here." },
  { id: "customers", label: "Customers", hint: "People already known. WhatsApp consent comes later." },
  { id: "profile", label: "Profile", hint: "Canonical name, industry, and voice." }
] as const;

type SectionId = (typeof SECTIONS)[number]["id"];

const CONTACT_KINDS = [
  { value: "Phone", label: "Phone" },
  { value: "Email", label: "Email" },
  { value: "Website", label: "Website" },
  { value: "SocialUrl", label: "Social URL" },
  { value: "DirectoryUrl", label: "Directory URL" }
];

const FACT_STATUSES = [
  { value: "Draft", label: "Draft" },
  { value: "Approved", label: "Approved" },
  { value: "Restricted", label: "Restricted" }
];

function coverage(data: BusinessIdentity) {
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
  return { filled: checks.filter(Boolean).length, total: checks.length };
}

export function BusinessIdentityPage() {
  const { businessId } = useParams();
  const query = useQuery({
    queryKey: ["identity", businessId],
    queryFn: () => api.identity(businessId!),
    enabled: Boolean(businessId)
  });

  if (!businessId) return <PageState mode="error" title="Business missing" />;
  if (query.isLoading) return <PageState mode="loading" title="Loading identity" />;
  if (query.isError || !query.data) {
    return <PageState mode="error" title="Identity unavailable" detail="Finish onboarding, then open this business from the dashboard." />;
  }

  return <IdentityWorkspace data={query.data} />;
}

function IdentityWorkspace({ data }: { data: BusinessIdentity }) {
  const [section, setSection] = useState<SectionId>("contacts");
  const current = SECTIONS.find((item) => item.id === section)!;
  const score = coverage(data);
  const industry = data.industries.find((item) => item.code === data.business.industryCode)?.name;

  return (
    <div className="workspace">
      <header className="workspace-head">
        <p className="hero-kicker">{String(score.filled).padStart(2, "0")} / {String(score.total).padStart(2, "0")} complete</p>
        <h1 className="display">{data.business.name}</h1>
        <p>{[industry, data.business.website?.replace(/^https?:\/\//, "")].filter(Boolean).join(" · ") || current.hint}</p>
        <nav className="segments" aria-label="Identity sections">
          {SECTIONS.map((item) => (
            <button
              key={item.id}
              type="button"
              aria-current={section === item.id ? "page" : undefined}
              className={section === item.id ? "is-on" : undefined}
              onClick={() => setSection(item.id)}
            >
              {item.label}
            </button>
          ))}
        </nav>
      </header>
      <p style={{ color: "var(--muted)", margin: 0 }}>{current.hint}</p>
      <AnimatePresence mode="wait">
        <motion.div key={section} initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.18 }}>
          {section === "profile" ? <ProfileCard data={data} /> : null}
          {section === "contacts" ? <ContactCard data={data} /> : null}
          {section === "categories" ? <CategoryCard data={data} /> : null}
          {section === "services" ? <ServiceCard data={data} /> : null}
          {section === "brands" ? <BrandCard data={data} /> : null}
          {section === "facts" ? <FactCard data={data} /> : null}
          {section === "customers" ? <CustomerCard data={data} /> : null}
        </motion.div>
      </AnimatePresence>
    </div>
  );
}

function Split({
  title,
  kicker,
  controls,
  records
}: {
  title: string;
  kicker: string;
  controls: ReactNode;
  records: ReactNode;
}) {
  return (
    <div className="workspace-split">
      <aside className="composer">
        <p className="kicker">{kicker}</p>
        <h3>{title}</h3>
        {controls}
      </aside>
      <div>{records}</div>
    </div>
  );
}

function Status({ error, ok }: { error: string | null; ok: boolean }) {
  return <Note error={error} ok={ok} />;
}

function LedgerSelect(props: { label: string; value: string; onChange: (value: string) => void; options: { value: string; label: string }[] }) {
  return <SelectField {...props} />;
}

function Actions({ children }: { children: ReactNode }) {
  return <span>{children}</span>;
}

function Action({ children, onClick, danger }: { children: ReactNode; onClick: () => void; danger?: boolean }) {
  return <GridAction danger={danger} onClick={onClick}>{children}</GridAction>;
}

function Kind({ children }: { children: ReactNode }) {
  return <span className="tag">{children}</span>;
}

function Pill({ status }: { status: string }) {
  const tone = status === "Approved" ? "ok" : status === "Restricted" ? "crit" : "hold";
  return <span className={`tag sev-${tone}`}>{status}</span>;
}

function useIdentityWork() {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState(false);

  async function run(action: () => Promise<unknown>) {
    setError(null);
    setOk(false);
    try {
      await action();
      await queryClient.invalidateQueries({ queryKey: ["identity"] });
      setOk(true);
      return true;
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "The change could not be saved.");
      return false;
    }
  }

  return { error, ok, run, setError };
}

function ProfileCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run } = useIdentityWork();
  const [name, setName] = useState(data.business.name);
  const [website, setWebsite] = useState(data.business.website ?? "");
  const [industry, setIndustry] = useState(data.business.industryCode ?? "");
  const [foundedYear, setFoundedYear] = useState(data.business.foundedYear?.toString() ?? "");
  const [brandVoice, setBrandVoice] = useState(data.business.brandVoice ?? "");
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    await run(() => api.updateBusinessProfile(data.business.id, {
      name,
      website,
      foundedYear: foundedYear ? Number(foundedYear) : null,
      brandVoice,
      industryCode: industry
    }));
    setBusy(false);
  }

  return (
    <form className="panel wizard-form" onSubmit={onSubmit}>
      <div className="band-2 band">
        <Field label="Business name" value={name} onChange={setName} required />
        <Field label="Website" value={website} onChange={setWebsite} hint="Used to compare listings later." />
        <LedgerSelect
          label="Industry"
          value={industry}
          onChange={setIndustry}
          options={[{ value: "", label: "Select industry" }, ...data.industries.map((item) => ({ value: item.code, label: item.name }))]}
        />
        <Field label="Founded year" value={foundedYear} onChange={setFoundedYear} type="number" />
        <div style={{ gridColumn: "1 / -1" }}>
          <AreaField label="Brand voice" value={brandVoice} onChange={setBrandVoice} />
        </div>
      </div>
      <div className="id-settings-foot">
        <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : "Save profile"}</Button>
        <Status error={error} ok={ok} />
      </div>
    </form>
  );
}

function ContactCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run, setError } = useIdentityWork();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [kind, setKind] = useState("Phone");
  const [value, setValue] = useState("");
  const [label, setLabel] = useState("");
  const [busy, setBusy] = useState(false);

  function clearForm() {
    setEditingId(null);
    setKind("Phone");
    setValue("");
    setLabel("");
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = value.trim();
    if (!trimmed) return;
    const duplicate = data.contacts.some((contact) =>
      contact.kind === kind && contact.value.trim() === trimmed && contact.id !== editingId);
    if (duplicate) {
      setError(`${kind} ${trimmed} is already recorded.`);
      return;
    }
    setBusy(true);
    const saved = await run(() => editingId
      ? api.updateContact(data.business.id, editingId, { kind, value: trimmed, label })
      : api.addContact(data.business.id, { kind, value: trimmed, label }));
    if (saved) clearForm();
    setBusy(false);
  }

  return (
    <Split
      kicker={editingId ? "Edit" : "New"}
      title={editingId ? "Edit contact" : "Add contact"}
      controls={(
        <form className="id-form" onSubmit={onSubmit}>
          <LedgerSelect label="Kind" value={kind} onChange={setKind} options={CONTACT_KINDS} />
          <Field label="Value" value={value} onChange={setValue} required />
          <Field label="Label" value={label} onChange={setLabel} hint="Optional. Example: Reception." />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : editingId ? "Save contact" : "Add contact"}</Button>
            {editingId ? <Button type="button" onClick={clearForm}>Cancel</Button> : null}
          </div>
          <Status error={error} ok={ok} />
        </form>
      )}
      records={(
        <DataGrid
          noun="contact"
          empty="Add the first phone, email, or listing URL."
          selectedId={editingId}
          columns={["Kind", "Value", "Label"]}
          rows={data.contacts.map((contact) => ({
            id: contact.id,
            search: `${contact.kind} ${contact.value} ${contact.label ?? ""}`.toLowerCase(),
            cells: [<Kind key="k">{contact.kind}</Kind>, contact.value, contact.label || "—"],
            actions: (
              <Actions>
                <Action onClick={() => { setEditingId(contact.id); setKind(contact.kind); setValue(contact.value); setLabel(contact.label ?? ""); }}>Edit</Action>
                <Action danger onClick={() => { void run(() => api.deleteContact(data.business.id, contact.id)).then((done) => { if (done && editingId === contact.id) clearForm(); }); }}>Remove</Action>
              </Actions>
            )
          }))}
        />
      )}
    />
  );
}

function CategoryCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run } = useIdentityWork();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [busy, setBusy] = useState(false);

  function clearForm() {
    setEditingId(null);
    setName("");
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!name.trim()) return;
    setBusy(true);
    const saved = await run(() => editingId
      ? api.updateCategory(data.business.id, editingId, name.trim())
      : api.addCategory(data.business.id, name.trim()));
    if (saved) clearForm();
    setBusy(false);
  }

  return (
    <Split
      kicker={editingId ? "Edit" : "New"}
      title={editingId ? "Edit category" : "Add category"}
      controls={(
        <form className="id-form" onSubmit={onSubmit}>
          <Field label="Category" value={name} onChange={setName} required />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : editingId ? "Save category" : "Add category"}</Button>
            {editingId ? <Button type="button" onClick={clearForm}>Cancel</Button> : null}
          </div>
          <Status error={error} ok={ok} />
        </form>
      )}
      records={(
        <DataGrid
          noun="category"
          empty="Add how this business should be classified."
          selectedId={editingId}
          columns={["Category"]}
          rows={data.categories.map((item) => ({
            id: item.id,
            search: item.name.toLowerCase(),
            cells: [item.name],
            actions: (
              <Actions>
                <Action onClick={() => { setEditingId(item.id); setName(item.name); }}>Edit</Action>
                <Action danger onClick={() => { void run(() => api.deleteCategory(data.business.id, item.id)).then((done) => { if (done && editingId === item.id) clearForm(); }); }}>Remove</Action>
              </Actions>
            )
          }))}
        />
      )}
    />
  );
}

function ServiceCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run } = useIdentityWork();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [busy, setBusy] = useState(false);

  function clearForm() {
    setEditingId(null);
    setName("");
    setDescription("");
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!name.trim()) return;
    setBusy(true);
    const body = { name: name.trim(), description };
    const saved = await run(() => editingId
      ? api.updateService(data.business.id, editingId, body)
      : api.addService(data.business.id, body));
    if (saved) clearForm();
    setBusy(false);
  }

  return (
    <Split
      kicker={editingId ? "Edit" : "New"}
      title={editingId ? "Edit service" : "Add service"}
      controls={(
        <form className="id-form" onSubmit={onSubmit}>
          <Field label="Service" value={name} onChange={setName} required />
          <Field label="Description" value={description} onChange={setDescription} />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : editingId ? "Save service" : "Add service"}</Button>
            {editingId ? <Button type="button" onClick={clearForm}>Cancel</Button> : null}
          </div>
          <Status error={error} ok={ok} />
        </form>
      )}
      records={(
        <DataGrid
          noun="service"
          empty="Add the first service this business offers."
          selectedId={editingId}
          columns={["Service", "Description"]}
          rows={data.services.map((service) => ({
            id: service.id,
            search: `${service.name} ${service.description ?? ""}`.toLowerCase(),
            cells: [service.name, service.description || "—"],
            actions: (
              <Actions>
                <Action onClick={() => { setEditingId(service.id); setName(service.name); setDescription(service.description ?? ""); }}>Edit</Action>
                <Action danger onClick={() => { void run(() => api.deleteService(data.business.id, service.id)).then((done) => { if (done && editingId === service.id) clearForm(); }); }}>Remove</Action>
              </Actions>
            )
          }))}
        />
      )}
    />
  );
}

function BrandCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run } = useIdentityWork();
  const [name, setName] = useState("");
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!name.trim()) return;
    setBusy(true);
    if (await run(() => api.addBrand(data.business.id, name.trim()))) setName("");
    setBusy(false);
  }

  return (
    <Split
      kicker="New"
      title="Add brand"
      controls={(
        <form className="id-form" onSubmit={onSubmit}>
          <Field label="Brand name" value={name} onChange={setName} required />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : "Add brand"}</Button>
          </div>
          <Status error={error} ok={ok} />
        </form>
      )}
      records={(
        <DataGrid
          noun="brand"
          empty="Add a trading name if it differs from the legal name."
          columns={["Brand"]}
          rows={data.brands.map((item) => ({
            id: item.id,
            search: item.name.toLowerCase(),
            cells: [item.name],
            actions: (
              <Actions>
                <Action danger onClick={() => { void run(() => api.deleteBrand(data.business.id, item.id)); }}>Remove</Action>
              </Actions>
            )
          }))}
        />
      )}
    />
  );
}

function FactCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run } = useIdentityWork();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [factType, setFactType] = useState(data.factTypes[0]?.code ?? "");
  const [value, setValue] = useState("");
  const [status, setStatus] = useState("Draft");
  const [busy, setBusy] = useState(false);

  function clearForm() {
    setEditingId(null);
    setFactType(data.factTypes[0]?.code ?? "");
    setValue("");
    setStatus("Draft");
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!value.trim()) return;
    setBusy(true);
    const body = { factTypeCode: factType, value: value.trim(), status };
    const saved = await run(() => editingId
      ? api.updateFact(data.business.id, editingId, body)
      : api.addFact(data.business.id, body));
    if (saved) clearForm();
    setBusy(false);
  }

  return (
    <Split
      kicker={editingId ? "Edit" : "New"}
      title={editingId ? "Edit fact" : "Add fact"}
      controls={(
        <form className="id-form" onSubmit={onSubmit}>
          <LedgerSelect label="Type" value={factType} onChange={setFactType} options={data.factTypes.map((item) => ({ value: item.code, label: item.name }))} />
          <Field label="Value" value={value} onChange={setValue} required />
          <LedgerSelect label="Status" value={status} onChange={setStatus} options={FACT_STATUSES} />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : editingId ? "Save fact" : "Add fact"}</Button>
            {editingId ? <Button type="button" onClick={clearForm}>Cancel</Button> : null}
          </div>
          <Status error={error} ok={ok} />
        </form>
      )}
      records={(
        <DataGrid
          noun="fact"
          empty="Record a claim only when you can stand behind it."
          selectedId={editingId}
          columns={["Type", "Value", "Status"]}
          rows={data.facts.map((fact) => ({
            id: fact.id,
            search: `${fact.factTypeCode} ${fact.value} ${fact.status}`.toLowerCase(),
            cells: [
              fact.factTypeCode,
              fact.value,
              <span key="s" className="id-status-cell">
                <Pill status={fact.status} />
                <small>{fact.canPublish ? "Publishable" : "Hold"}</small>
              </span>
            ],
            actions: (
              <Actions>
                <Action onClick={() => { setEditingId(fact.id); setFactType(fact.factTypeCode); setValue(fact.value); setStatus(fact.status); }}>Edit</Action>
                {fact.status !== "Approved" ? (
                  <Action onClick={() => { void run(() => api.updateFact(data.business.id, fact.id, { factTypeCode: fact.factTypeCode, value: fact.value, status: "Approved" })); }}>Approve</Action>
                ) : null}
                {fact.status !== "Restricted" ? (
                  <Action onClick={() => { void run(() => api.updateFact(data.business.id, fact.id, { factTypeCode: fact.factTypeCode, value: fact.value, status: "Restricted" })); }}>Restrict</Action>
                ) : null}
                <Action danger onClick={() => { void run(() => api.deleteFact(data.business.id, fact.id)).then((done) => { if (done && editingId === fact.id) clearForm(); }); }}>Remove</Action>
              </Actions>
            )
          }))}
        />
      )}
    />
  );
}

function CustomerCard({ data }: { data: BusinessIdentity }) {
  const { error, ok, run } = useIdentityWork();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [mobile, setMobile] = useState("");
  const [email, setEmail] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);

  function clearForm() {
    setEditingId(null);
    setDisplayName("");
    setMobile("");
    setEmail("");
    setNotes("");
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!displayName.trim() || !mobile.trim()) return;
    setBusy(true);
    const body = { displayName: displayName.trim(), mobile: mobile.trim(), email, notes };
    const saved = await run(() => editingId
      ? api.updateCustomer(data.business.id, editingId, body)
      : api.addCustomer(data.business.id, body));
    if (saved) clearForm();
    setBusy(false);
  }

  return (
    <Split
      kicker={editingId ? "Edit" : "New"}
      title={editingId ? "Edit customer" : "Add customer"}
      controls={(
        <form className="id-form" onSubmit={onSubmit}>
          <Field label="Name" value={displayName} onChange={setDisplayName} required />
          <Field label="Mobile" value={mobile} onChange={setMobile} required />
          <Field label="Email" value={email} onChange={setEmail} type="email" />
          <Field label="Notes" value={notes} onChange={setNotes} />
          <div className="id-form-actions">
            <Button appearance="primary" type="submit" disabled={busy}>{busy ? "Saving…" : editingId ? "Save customer" : "Add customer"}</Button>
            {editingId ? <Button type="button" onClick={clearForm}>Cancel</Button> : null}
          </div>
          <Status error={error} ok={ok} />
        </form>
      )}
      records={(
        <DataGrid
          noun="customer"
          empty="Add a person the business already knows. Messaging consent comes later."
          selectedId={editingId}
          columns={["Name", "Mobile", "Email", "Notes"]}
          rows={data.customers.map((customer) => {
            const phone = customer.contacts.find((contact) => contact.kind === "Mobile")?.value ?? "—";
            const mail = customer.contacts.find((contact) => contact.kind === "Email")?.value ?? "—";
            return {
              id: customer.id,
              search: `${customer.displayName} ${phone} ${mail} ${customer.notes ?? ""}`.toLowerCase(),
              cells: [customer.displayName, phone, mail, customer.notes || "—"],
              actions: (
                <Actions>
                  <Action onClick={() => {
                    setEditingId(customer.id);
                    setDisplayName(customer.displayName);
                    setMobile(customer.contacts.find((contact) => contact.kind === "Mobile")?.value ?? "");
                    setEmail(customer.contacts.find((contact) => contact.kind === "Email")?.value ?? "");
                    setNotes(customer.notes ?? "");
                  }}>Edit</Action>
                  <Action danger onClick={() => { void run(() => api.deleteCustomer(data.business.id, customer.id)).then((done) => { if (done && editingId === customer.id) clearForm(); }); }}>Remove</Action>
                </Actions>
              )
            };
          })}
        />
      )}
    />
  );
}
