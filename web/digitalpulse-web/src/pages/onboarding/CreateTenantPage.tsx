import { Button, Input, Label, Radio, RadioGroup } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { useSession } from "../../state/session";
import { PageState } from "../../components/PageState";

export function CreateTenantPage() {
  const navigate = useNavigate();
  const applyAuth = useSession((s) => s.applyAuth);
  const [type, setType] = useState("Direct");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const auth = await api.createTenant({ name: String(form.get("name") ?? ""), type });
      applyAuth(auth);
      navigate("/onboarding/tenant-review");
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not create tenant.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Wizard title="Create tenant" step="1 / 5">
      <form className="grid gap-4" onSubmit={onSubmit}>
        <label className="grid gap-2">
          <Label>Workspace name</Label>
          <Input name="name" required placeholder="Harbour Studio" />
        </label>
        <RadioGroup value={type} onChange={(_, d) => setType(d.value)} layout="horizontal">
          <Radio value="Direct" label="Direct customer" />
          <Radio value="Agency" label="Agency" />
        </RadioGroup>
        {error ? <PageState mode="error" title="Tenant was not created" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>Continue</Button>
      </form>
    </Wizard>
  );
}

export function Wizard({ title, step, children }: { title: string; step: string; children: React.ReactNode }) {
  return (
    <main className="mx-auto max-w-xl px-5 py-10">
      <p className="text-xs uppercase tracking-[0.24em]" style={{ color: "var(--signal)" }}>{step}</p>
      <h1 className="display mb-6 mt-2 text-4xl">{title}</h1>
      <div className="rounded-3xl border p-6" style={{ background: "var(--card)", borderColor: "var(--stroke)" }}>
        {children}
      </div>
    </main>
  );
}
