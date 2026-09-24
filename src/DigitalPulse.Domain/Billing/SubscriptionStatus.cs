namespace DigitalPulse.Domain.Billing;

public enum SubscriptionStatus
{
    Active = 0,
    Cancelled = 1,
    Trial = 2,
    PastDue = 3,
    Suspended = 4
}
