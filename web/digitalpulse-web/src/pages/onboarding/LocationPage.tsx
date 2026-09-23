import { Button, Input, Label } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { PageState } from "../../components/PageState";
import { Wizard } from "./CreateTenantPage";

export function LocationPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const businessId = sessionStorage.getItem("dp.businessId");
    if (!businessId) {
      setError("Create a business first.");
      return;
    }
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    try {
      await api.createLocation(businessId, {
        name: String(form.get("name") ?? ""),
        addressLine: String(form.get("addressLine") ?? ""),
        city: String(form.get("city") ?? ""),
        region: String(form.get("region") ?? ""),
        postalCode: String(form.get("postalCode") ?? ""),
        countryCode: String(form.get("countryCode") ?? "IN")
      });
      navigate("/onboarding/plan");
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not save location.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Wizard title="Location" step="4 / 5">
      <form className="grid gap-4" onSubmit={onSubmit}>
        <label className="grid gap-2"><Label>Location name</Label><Input name="name" required placeholder="Flagship" /></label>
        <label className="grid gap-2"><Label>Address</Label><Input name="addressLine" /></label>
        <div className="grid gap-4 md:grid-cols-2">
          <label className="grid gap-2"><Label>City</Label><Input name="city" /></label>
          <label className="grid gap-2"><Label>Region</Label><Input name="region" /></label>
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          <label className="grid gap-2"><Label>Postal code</Label><Input name="postalCode" /></label>
          <label className="grid gap-2"><Label>Country</Label><Input name="countryCode" defaultValue="IN" maxLength={2} /></label>
        </div>
        {error ? <PageState mode="error" title="Location not saved" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>Choose a plan</Button>
      </form>
    </Wizard>
  );
}
