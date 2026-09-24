import { Button, Input } from "@fluentui/react-components";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api, type BusinessResponse, type LocationResponse } from "../lib/api";
import { PageState } from "../components/PageState";
import { useSession } from "../state/session";

export function WorkspacePage() {
  const profile = useSession((s) => s.profile);
  const tenantQuery = useQuery({ queryKey: ["tenant"], queryFn: api.currentTenant });
  const businessesQuery = useQuery({ queryKey: ["businesses"], queryFn: api.listBusinesses });
  const business = businessesQuery.data?.[0];
  const locationsQuery = useQuery({
    queryKey: ["locations", business?.id],
    queryFn: () => api.listLocations(business!.id),
    enabled: Boolean(business?.id)
  });

  if (tenantQuery.isLoading || businessesQuery.isLoading) {
    return <PageState mode="loading" title="Loading workspace" />;
  }
  if (tenantQuery.isError || businessesQuery.isError || !tenantQuery.data) {
    return <PageState mode="error" title="Could not load workspace" detail="Finish onboarding first, then return here to update details." />;
  }

  return (
    <main className="command">
      <div>
        <p className="hero-kicker">Workspace</p>
        <h1 className="page-title">Update info</h1>
        <p className="page-lead">Change the details captured during onboarding. This does not restart the flow.</p>
      </div>
      <ProfileForm displayName={profile?.displayName ?? ""} email={profile?.email ?? ""} />
      <TenantForm name={tenantQuery.data.name} type={tenantQuery.data.type} />
      {business ? <BusinessForm business={business} /> : <PageState mode="empty" title="No business yet" />}
      {business && locationsQuery.data?.map((location) => (
        <LocationForm key={location.id} businessId={business.id} location={location} />
      ))}
    </main>
  );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="panel">
      <h2>{title}</h2>
      {children}
    </section>
  );
}

function Status({ error, ok }: { error: string | null; ok: boolean }) {
  if (error) return <p className="text-sm note-err" role="alert">{error}</p>;
  if (ok) return <p className="text-sm note-ok">Saved.</p>;
  return null;
}

function ProfileForm({ displayName, email }: { displayName: string; email: string }) {
  const queryClient = useQueryClient();
  const hydrate = useSession((s) => s.hydrate);
  const [error, setError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (name: string) => api.updateProfile(name),
    onSuccess: async () => {
      await hydrate();
      await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    }
  });

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError(null);
    try {
      await mutation.mutateAsync(String(form.get("displayName") ?? ""));
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not update profile.");
    }
  }

  return (
    <Card title="Your profile">
      <form className="form-stack" onSubmit={onSubmit}>
        <label className="dp-field"><span>Name</span><Input name="displayName" defaultValue={displayName} required /></label>
        <label className="dp-field"><span>Email</span><Input value={email} disabled /></label>
        <div className="id-form-actions">
          <Button appearance="primary" type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Saving…" : "Save profile"}</Button>
        </div>
        <Status error={error} ok={mutation.isSuccess && !error} />
      </form>
    </Card>
  );
}

function TenantForm({ name, type }: { name: string; type: string }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (next: string) => api.updateTenant(next),
    onSuccess: () => queryClient.invalidateQueries()
  });

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    try {
      await mutation.mutateAsync(String(new FormData(event.currentTarget).get("name") ?? ""));
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not update tenant.");
    }
  }

  return (
    <Card title="Tenant">
      <form className="form-stack" onSubmit={onSubmit}>
        <label className="dp-field"><span>Workspace name</span><Input name="name" defaultValue={name} required /></label>
        <p className="ink-muted">Type: {type}</p>
        <div className="id-form-actions">
          <Button appearance="primary" type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Saving…" : "Save tenant"}</Button>
        </div>
        <Status error={error} ok={mutation.isSuccess && !error} />
      </form>
    </Card>
  );
}

function BusinessForm({ business }: { business: BusinessResponse }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (body: { name: string; website?: string }) => api.updateBusiness(business.id, body),
    onSuccess: () => queryClient.invalidateQueries()
  });

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError(null);
    try {
      await mutation.mutateAsync({
        name: String(form.get("name") ?? ""),
        website: String(form.get("website") ?? "")
      });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not update business.");
    }
  }

  return (
    <Card title="Business">
      <form className="form-stack" onSubmit={onSubmit}>
        <label className="dp-field"><span>Business name</span><Input name="name" defaultValue={business.name} required /></label>
        <label className="dp-field"><span>Website</span><Input name="website" defaultValue={business.website ?? ""} /></label>
        <div className="id-form-actions">
          <Button appearance="primary" type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Saving…" : "Save business"}</Button>
          <Button appearance="subtle" type="button" onClick={() => navigate(`/app/businesses/${business.id}`)}>Open identity</Button>
        </div>
        <Status error={error} ok={mutation.isSuccess && !error} />
      </form>
    </Card>
  );
}

function LocationForm({ businessId, location }: { businessId: string; location: LocationResponse }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (body: Record<string, string>) => api.updateLocation(businessId, location.id, body),
    onSuccess: () => queryClient.invalidateQueries()
  });

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError(null);
    try {
      await mutation.mutateAsync({
        name: String(form.get("name") ?? ""),
        addressLine: String(form.get("addressLine") ?? ""),
        city: String(form.get("city") ?? ""),
        region: String(form.get("region") ?? ""),
        postalCode: String(form.get("postalCode") ?? ""),
        countryCode: String(form.get("countryCode") ?? "IN")
      });
    } catch (err) {
      setError(err instanceof ApiError ? err.title : "Could not update location.");
    }
  }

  return (
    <Card title="Location">
      <form className="form-stack" onSubmit={onSubmit}>
        <label className="dp-field"><span>Location name</span><Input name="name" defaultValue={location.name} required /></label>
        <label className="dp-field"><span>Address</span><Input name="addressLine" defaultValue={location.addressLine ?? ""} /></label>
        <div className="form-grid">
          <label className="dp-field"><span>City</span><Input name="city" defaultValue={location.city ?? ""} /></label>
          <label className="dp-field"><span>Region</span><Input name="region" defaultValue={location.region ?? ""} /></label>
          <label className="dp-field"><span>Postal code</span><Input name="postalCode" defaultValue={location.postalCode ?? ""} /></label>
          <label className="dp-field"><span>Country</span><Input name="countryCode" defaultValue={location.countryCode} maxLength={2} /></label>
        </div>
        <div className="id-form-actions">
          <Button appearance="primary" type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Saving…" : "Save location"}</Button>
        </div>
        <Status error={error} ok={mutation.isSuccess && !error} />
      </form>
    </Card>
  );
}
