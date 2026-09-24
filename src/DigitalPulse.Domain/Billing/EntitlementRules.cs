using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Domain.Billing;

public static class EntitlementRules
{
    public static void EnsureUsable(SubscriptionStatus status)
    {
        if (!BillingPolicy.IsUsable(status))
        {
            throw new InvalidOperationException("Subscription must be Active or Trial to use paid entitlements.");
        }
    }

    public static void EnsureCanAddBusiness(TenantType tenantType, SubscriptionPlan plan, int currentBusinessCount)
    {
        Subscription.EnsurePlanMatchesTenant(tenantType, plan);
        var cap = BillingPolicy.BusinessCap(tenantType, plan);
        if (currentBusinessCount >= cap)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {cap} business(es).");
        }
    }

    public static void EnsureCanAddLocation(SubscriptionPlan plan, int currentLocationCount)
    {
        var cap = BillingPolicy.LocationCap(plan);
        if (currentLocationCount >= cap)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {cap} location(s).");
        }
    }

    public static void EnsureCanAddUser(SubscriptionPlan plan, int currentUserCount)
    {
        if (currentUserCount >= plan.MaxUsers)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.MaxUsers} user(s).");
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

    public static void EnsureCanRunAi(SubscriptionPlan plan, int generationsThisMonth)
    {
        if (generationsThisMonth >= plan.AiGenerationsPerMonth)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.AiGenerationsPerMonth} AI generation(s) this month.");
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
