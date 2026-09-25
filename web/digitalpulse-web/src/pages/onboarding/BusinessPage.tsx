import { Button } from "../../design/Button";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { PageState } from "../../components/PageState";
import { Field } from "../../design/Field";
import { Wizard } from "./CreateTenantPage";

export function BusinessPage() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [website, setWebsite] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const business = await api.createBusiness({
        name,
        website
      });
      sessionStorage.setItem("dp.businessId", business.id);
      navigate("/onboarding/location");
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not save business.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Wizard title="Business" step="3 / 5">
      <form className="wizard-form" onSubmit={onSubmit}>
        <Field label="Business name" value={name} onChange={setName} required />
        <Field label="Website" value={website} onChange={setWebsite} type="url" />
        {error ? <PageState mode="error" title="Business not saved" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>Continue</Button>
      </form>
    </Wizard>
  );
}
