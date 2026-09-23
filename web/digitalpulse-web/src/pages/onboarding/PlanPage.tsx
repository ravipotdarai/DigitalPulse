import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../../lib/api";
import { PageState } from "../../components/PageState";
import { Wizard } from "./CreateTenantPage";

export function PlanPage() {
  const navigate = useNavigate();
  const query = useQuery({ queryKey: ["plans"], queryFn: api.plans });
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  if (query.isLoading) return <PageState mode="loading" title="Loading plans" />;
  if (query.isError) return <PageState mode="error" title="Plans unavailable" />;
  if (!query.data?.length) return <PageState mode="empty" title="No plans available" />;

  return (
    <Wizard title="Select a plan" step="5 / 5">
      <div className="grid gap-3">
        {query.data.map((plan) => (
          <button
            key={plan.code}
            type="button"
            disabled={busy !== null}
            onClick={async () => {
              setBusy(plan.code);
              setError(null);
              try {
                await api.selectPlan(plan.code);
                navigate("/app");
              } catch (err) {
                setError(err instanceof ApiError ? err.title : "Could not select plan.");
              } finally {
                setBusy(null);
              }
            }}
            className="rounded-2xl border p-4 text-left transition hover:-translate-y-0.5"
            style={{ borderColor: "var(--stroke)", background: "transparent" }}
          >
            <div className="flex items-baseline justify-between">
              <span className="display text-2xl">{plan.name}</span>
              <span style={{ color: "var(--signal)" }}>₹{plan.monthlyPriceInr.toLocaleString("en-IN")}/mo</span>
            </div>
            <p className="mt-1 text-sm" style={{ color: "var(--muted)" }}>
              {plan.maxBusinesses} business{plan.maxBusinesses === 1 ? "" : "es"} · billed later
            </p>
            {busy === plan.code ? <p className="mt-2 text-xs">Saving…</p> : null}
          </button>
        ))}
      </div>
      {error ? <PageState mode="error" title="Plan not saved" detail={error} /> : null}
      <p className="mt-4 text-xs" style={{ color: "var(--muted)" }}>Plans come from SQL seed data. Payments are not collected in this slice.</p>
    </Wizard>
  );
}
