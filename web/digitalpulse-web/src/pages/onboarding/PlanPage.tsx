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

  const recommended = query.data.find((plan) => plan.maxBusinesses === 3)?.code;

  return (
    <Wizard title="Select a plan" step="5 / 5">
      <p className="lead">Prices and entitlements come from the plan catalog. Checkout stays held until a live billing provider confirms payment.</p>
      <div className="plan-board">
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
            className={plan.code === recommended ? "plan-card is-rec" : "plan-card"}
          >
            <span className="hero-kicker">{plan.code === recommended ? "Recommended" : plan.agencyOnly ? "Agency" : "Direct"}</span>
            <strong>{plan.name}</strong>
            <em>₹{plan.monthlyPriceInr.toLocaleString("en-IN")} / month</em>
            <p className="ink-muted flush">
              {plan.maxBusinesses} business{plan.maxBusinesses === 1 ? "" : "es"}
              {plan.agencyOnly ? " · agencies" : ""}
            </p>
            {busy === plan.code ? <p className="mono meta-line" role="status">Saving…</p> : null}
          </button>
        ))}
      </div>
      {error ? <PageState mode="error" title="Plan not saved" detail={error} /> : null}
    </Wizard>
  );
}
