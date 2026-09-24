using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Domain.Billing;

public static class EntitlementRules
{
    public static void EnsureCanAddBusiness(TenantType tenantType, SubscriptionPlan plan, int currentBusinessCount)
    {
        Subscription.EnsurePlanMatchesTenant(tenantType, plan);
        if (currentBusinessCount >= plan.MaxBusinesses)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.MaxBusinesses} business(es).");
        }
    }

    public static void EnsureCanAddConnection(SubscriptionPlan plan, int currentConnectionCount)
    {
        if (currentConnectionCount >= plan.MaxConnections)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.MaxConnections} connection(s).");
        }
    }

    public static void EnsureCanRunScan(SubscriptionPlan plan, int scansThisMonth)
    {
        if (scansThisMonth >= plan.ScansPerMonth)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.ScansPerMonth} scan(s) this month.");
        }
    }

    public static void EnsureCanRunAction(SubscriptionPlan plan, int actionsThisMonth)
    {
        if (actionsThisMonth >= plan.ActionsPerMonth)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.ActionsPerMonth} action(s) this month.");
        }
    }

    public static void EnsureCanUseWhatsApp(SubscriptionPlan plan)
    {
        if (!plan.WhatsAppEnabled)
        {
            throw new InvalidOperationException($"Plan {plan.Code} does not include WhatsApp Business Messaging.");
        }
    }

    public static void EnsureCanSendWhatsApp(SubscriptionPlan plan, int messagesThisMonth)
    {
        EnsureCanUseWhatsApp(plan);
        if (messagesThisMonth >= plan.WhatsAppMessagesPerMonth)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.WhatsAppMessagesPerMonth} WhatsApp message(s) this month.");
        }
    }
}
