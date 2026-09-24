using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Domain.Billing;

public sealed class Subscription : TenantOwnedEntity
{
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public BillingInterval Interval { get; private set; } = BillingInterval.Monthly;
    public DateTimeOffset PeriodStartUtc { get; private set; } = BillingPolicy.PeriodStart();
    public DateTimeOffset PeriodEndUtc { get; private set; } = BillingPolicy.PeriodEnd(BillingInterval.Monthly);
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? ProviderCode { get; private set; }
    public string? ProviderSubscriptionId { get; private set; }
    public string HoldReason { get; private set; } = "Plan selected. Payment stays held until a live billing provider confirms it.";

    public bool IsUsable => BillingPolicy.IsUsable(Status);

    private Subscription() { }

    public static Subscription Start(Guid tenantId, Guid planId, BillingInterval interval = BillingInterval.Monthly)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (planId == Guid.Empty) throw new ArgumentException("Plan is required.", nameof(planId));
        var start = BillingPolicy.PeriodStart();
        return new Subscription
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            Interval = interval,
            PeriodStartUtc = start,
            PeriodEndUtc = BillingPolicy.PeriodEnd(interval, start),
            HoldReason = "Plan selected. Payment stays held until a live billing provider confirms it."
        };
    }

    public void ChangePlan(Guid planId, BillingInterval? interval = null)
    {
        if (planId == Guid.Empty) throw new ArgumentException("Plan is required.", nameof(planId));
        if (!IsUsable)
        {
            throw new InvalidOperationException("Resume or start a usable subscription before changing plan.");
        }

        PlanId = planId;
        if (interval is { } next)
        {
            Interval = next;
            PeriodEndUtc = BillingPolicy.PeriodEnd(next, PeriodStartUtc);
        }

        HoldReason = "Plan change recorded. The next invoice stays held until a live provider captures payment.";
        Touch();
    }

    public void RequestCancel()
    {
        if (!IsUsable)
        {
            throw new InvalidOperationException("Only an Active or Trial subscription can be cancelled.");
        }

        CancelAtPeriodEnd = true;
        HoldReason = "Cancellation is scheduled at period end. Paid entitlements stay usable until then.";
        Touch();
    }

    public void CancelNow(string reason)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelAtPeriodEnd = false;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        HoldReason = string.IsNullOrWhiteSpace(reason)
            ? "Subscription cancelled. Metered work is blocked."
            : reason.Trim();
        Touch();
    }

    public void Resume()
    {
        if (Status == SubscriptionStatus.Cancelled && CancelledAtUtc is { } at && at < DateTimeOffset.UtcNow.AddDays(-30))
        {
            throw new InvalidOperationException("This cancelled subscription is too old to resume. Select a plan again.");
        }

        if (Status is not (SubscriptionStatus.Cancelled or SubscriptionStatus.PastDue or SubscriptionStatus.Suspended)
            && !CancelAtPeriodEnd)
        {
            throw new InvalidOperationException("This subscription is already usable.");
        }

        Status = SubscriptionStatus.Active;
        CancelAtPeriodEnd = false;
        CancelledAtUtc = null;
        HoldReason = "Subscription resumed. Live payment capture still waits for the billing provider.";
        Touch();
    }

    public void MarkPastDue(string detail)
    {
        Status = SubscriptionStatus.PastDue;
        HoldReason = detail.Trim();
        Touch();
    }

    public void Suspend(string detail)
    {
        Status = SubscriptionStatus.Suspended;
        HoldReason = detail.Trim();
        Touch();
    }

    public void Renew(DateTimeOffset? now = null)
    {
        if (CancelAtPeriodEnd)
        {
            CancelNow("Period ended after a scheduled cancellation.");
            return;
        }

        if (!IsUsable)
        {
            throw new InvalidOperationException("Only a usable subscription can renew.");
        }

        var start = BillingPolicy.PeriodStart(now);
        PeriodStartUtc = start;
        PeriodEndUtc = BillingPolicy.PeriodEnd(Interval, start);
        HoldReason = "Period renewed. The renewal invoice stays held without a live capture.";
        Touch();
    }

    public void AttachProvider(string provider, string? providerSubscriptionId)
    {
        ProviderCode = provider.Trim();
        ProviderSubscriptionId = string.IsNullOrWhiteSpace(providerSubscriptionId) ? null : providerSubscriptionId.Trim();
        Touch();
    }

    public static void EnsurePlanMatchesTenant(TenantType tenantType, SubscriptionPlan plan)
    {
        if (plan.AgencyOnly && tenantType != TenantType.Agency)
        {
            throw new InvalidOperationException("The Agency plan can only be assigned to Agency tenants.");
        }

        if (!plan.AgencyOnly && tenantType == TenantType.Agency)
        {
            throw new InvalidOperationException("Agency tenants must use the Agency plan.");
        }
    }
}
