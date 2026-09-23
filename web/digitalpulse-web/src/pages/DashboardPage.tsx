import { Button } from "@fluentui/react-components";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { PageState } from "../components/PageState";

export function DashboardPage() {
  const navigate = useNavigate();
  const query = useQuery({ queryKey: ["dashboard"], queryFn: api.dashboard });

  if (query.isLoading) return <PageState mode="loading" title="Opening observatory" />;
  if (query.isError) return <PageState mode="error" title="Dashboard unavailable" detail="Finish onboarding if you have not selected a plan." />;
  const data = query.data!;

  return (
    <main className="mx-auto max-w-5xl px-5 py-8 md:px-8">
      <p className="text-xs uppercase tracking-[0.24em]" style={{ color: "var(--signal)" }}>{data.tenantType} tenant</p>
      <div className="mt-2 flex flex-wrap items-end justify-between gap-3">
        <h1 className="display text-5xl">{data.tenantName}</h1>
        <Button appearance="primary" onClick={() => navigate("/app/info")}>Update info</Button>
      </div>
      <p className="mt-2" style={{ color: "var(--muted)" }}>{data.planName} · {data.businessCount}/{data.maxBusinesses} businesses · {data.locationCount} locations</p>
      <div className="mt-8 grid gap-4 md:grid-cols-2">
        {data.businesses.length === 0 ? <PageState mode="empty" title="No businesses yet" /> : data.businesses.map((business) => (
          <article key={business.id} className="rounded-3xl border p-5" style={{ background: "var(--card)", borderColor: "var(--stroke)" }}>
            <h2 className="display text-2xl">{business.name}</h2>
            <p className="mt-1 text-sm" style={{ color: "var(--muted)" }}>{business.website ?? "No website yet"}</p>
            <p className="mt-4 text-sm">{business.locationCount} location{business.locationCount === 1 ? "" : "s"}</p>
            <Button className="mt-4" appearance="subtle" onClick={() => navigate("/app/info")}>Edit details</Button>
          </article>
        ))}
      </div>
    </main>
  );
}
