import { Button, Input, Label } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { PageState } from "../../components/PageState";
import { Wizard } from "./CreateTenantPage";

export function BusinessPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const business = await api.createBusiness({
        name: String(form.get("name") ?? ""),
        website: String(form.get("website") ?? "")
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
      <form className="grid gap-4" onSubmit={onSubmit}>
        <label className="grid gap-2"><Label>Business name</Label><Input name="name" required /></label>
        <label className="grid gap-2"><Label>Website</Label><Input name="website" type="url" placeholder="https://" /></label>
        {error ? <PageState mode="error" title="Business not saved" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>Continue</Button>
      </form>
    </Wizard>
  );
}
