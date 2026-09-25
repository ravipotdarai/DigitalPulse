import { Button } from "../design/Button";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, api, type BillingWorkspace } from "../lib/api";
import { PageState } from "../components/PageState";
import { DataGrid } from "../design/DataGrid";

export function BillingPage() {
  const query = useQuery({ queryKey: ["billing"], queryFn: api.billing });
  if (query.isLoading) return <PageState mode="loading" title="Opening billing" />;
  if (query.isError || !query.data) return <PageState mode="error" title="Billing workspace unavailable" />;
  return <BillingWorkspaceView data={query.data} />;
}

function BillingWorkspaceView({ data }: { data: BillingWorkspace }) {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [planCode, setPlanCode] = useState(data.plan?.code ?? data.availablePlans[0]?.code ?? "STARTER");

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["billing"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    await queryClient.invalidateQueries({ queryKey: ["plans"] });
  }

  async function run(action: () => Promise<unknown>, ok: string) {
    setError(null);
    try {
      await action();
      setSuccess(ok);
      await refresh();
    } catch (err) {
      setSuccess(null);
      setError(err instanceof ApiError ? err.title : "Billing could not continue.");
    }
  }

  return (
    <section className="command">
      <header>
        <p className="hero-kicker">Billing + SaaS</p>
        <h1 className="display display-page command-title">Subscription</h1>
        <p className="ink-muted command-lead">{data.note}</p>
        <p className="ink-muted">
          {data.subscription ? `${data.subscription.planName} · ${data.subscription.status} · ${data.subscription.interval}` : "No plan selected"}
          {" · "}
          {data.providerIsLive ? data.providerName : `${data.providerName} (held)`}
        </p>
        {data.subscription ? <p className="ink-muted">{data.subscription.holdReason}</p> : null}
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        {success ? <p className="note-ok">{success}</p> : null}
      </header>

      <div className="band band-2">
        <article className="panel">
          <h2>Change plan</h2>
          <form
            className="id-form"
            onSubmit={(event) => {
              event.preventDefault();
              run(() => api.changePlan({ planCode, interval: "Monthly" }), "Plan change recorded. The invoice stays held.");
            }}
          >
            <label className="dp-field">
              <span>Catalog plan</span>
              <select value={planCode} onChange={(event) => setPlanCode(event.target.value)}>
                {data.availablePlans.map((plan) => (
                  <option key={plan.code} value={plan.code}>
                    {plan.name} · ₹{plan.monthlyPriceInr.toLocaleString("en-IN")}/mo
                  </option>
                ))}
              </select>
            </label>
            <div className="id-form-actions spaced">
              <Button appearance="primary" type="submit">Change plan</Button>
              <Button appearance="secondary" type="button" onClick={() => run(() => api.checkoutBilling({ invoiceId: null }), "Checkout asked the billing provider. A capture was not invented.")}>
                Checkout
              </Button>
            </div>
          </form>
        </article>
        <article className="panel">
          <h2>Lifecycle</h2>
          <p className="ink-muted">Cancel at period end keeps entitlements until the period closes. Immediate cancel blocks metered work.</p>
          <div className="id-form-actions spaced">
            <Button appearance="secondary" onClick={() => run(() => api.cancelBilling({ immediately: false, reason: null }), "Cancellation scheduled at period end.")}>
              Cancel at period end
            </Button>
            <Button appearance="secondary" onClick={() => run(() => api.cancelBilling({ immediately: true, reason: "Cancelled from the billing workspace." }), "Subscription cancelled.")}>
              Cancel now
            </Button>
            <Button appearance="primary" onClick={() => run(() => api.resumeBilling(), "Subscription resumed. Live capture still waits.")}>
              Resume
            </Button>
          </div>
        </article>
      </div>

      <article className="panel">
        <h2>Usage this month</h2>
        <DataGrid
          noun="meter"
          empty="Select a plan to see usage against catalog entitlements."
          columns={["Meter", "Used", "Included", "Note"]}
          rows={data.usage.map((item) => ({
            id: item.kind,
            search: item.kind.toLowerCase(),
            cells: [item.kind, String(item.used), String(item.included), item.note]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Invoices</h2>
        <DataGrid
          noun="invoice"
          empty="Select a plan to issue a held invoice."
          columns={["Number", "Status", "Amount", "Hold"]}
          rows={data.invoices.map((item) => ({
            id: item.id,
            search: `${item.number} ${item.status}`.toLowerCase(),
            cells: [item.number, item.status, `₹${item.amountInr.toLocaleString("en-IN")}`, item.holdReason]
          }))}
        />
      </article>

      <article className="panel">
        <h2>Payment attempts</h2>
        <DataGrid
          noun="attempt"
          empty="Checkout to record a held provider attempt."
          columns={["Provider", "Status", "Detail"]}
          rows={data.payments.map((item) => ({
            id: item.id,
            search: `${item.provider} ${item.status}`.toLowerCase(),
            cells: [item.provider, item.status, item.detail]
          }))}
        />
      </article>
    </section>
  );
}
