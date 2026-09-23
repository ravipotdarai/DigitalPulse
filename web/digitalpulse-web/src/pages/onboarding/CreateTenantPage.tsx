import { Button, Radio, RadioGroup } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { useSession } from "../../state/session";
import { PageState } from "../../components/PageState";
import { Field } from "../../design/Field";
import { Wizard } from "../../design/OnboardingFrame";

export { Wizard };

export function CreateTenantPage() {
  const navigate = useNavigate();
  const applyAuth = useSession((s) => s.applyAuth);
  const [type, setType] = useState("Direct");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const auth = await api.createTenant({ name, type });
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
      <form className="wizard-form" onSubmit={onSubmit}>
        <Field label="Workspace name" value={name} onChange={setName} required />
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
