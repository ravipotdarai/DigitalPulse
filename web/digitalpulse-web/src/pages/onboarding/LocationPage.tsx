import { Button } from "@fluentui/react-components";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { PageState } from "../../components/PageState";
import { Field } from "../../design/Field";
import { Wizard } from "./CreateTenantPage";

export function LocationPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [name, setName] = useState("");
  const [addressLine, setAddressLine] = useState("");
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [countryCode, setCountryCode] = useState("IN");

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const businessId = sessionStorage.getItem("dp.businessId");
    if (!businessId) {
      setError("Create a business first.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await api.createLocation(businessId, { name, addressLine, city, region, postalCode, countryCode });
      navigate("/onboarding/plan");
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not save location.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Wizard title="Location" step="4 / 5">
      <form className="wizard-form" onSubmit={onSubmit}>
        <Field label="Location name" value={name} onChange={setName} required />
        <Field label="Address" value={addressLine} onChange={setAddressLine} />
        <div className="band band-2">
          <Field label="City" value={city} onChange={setCity} />
          <Field label="Region" value={region} onChange={setRegion} />
          <Field label="Postal code" value={postalCode} onChange={setPostalCode} />
          <Field label="Country" value={countryCode} onChange={setCountryCode} />
        </div>
        {error ? <PageState mode="error" title="Location not saved" detail={error} /> : null}
        <Button appearance="primary" type="submit" disabled={busy}>Choose a plan</Button>
      </form>
    </Wizard>
  );
}
