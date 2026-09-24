using DigitalPulse.Domain.Actions;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Domain.Directories;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Domain.Social;
using DigitalPulse.Domain.Website;
using DigitalPulse.Domain.WhatsApp;
using DigitalPulse.Domain.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantMembership> Memberships { get; }
    DbSet<Business> Businesses { get; }
    DbSet<BusinessLocation> Locations { get; }
    DbSet<SubscriptionPlan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Industry> Industries { get; }
    DbSet<FactType> FactTypes { get; }
    DbSet<ContactPoint> ContactPoints { get; }
    DbSet<BusinessCategory> Categories { get; }
    DbSet<Service> Services { get; }
    DbSet<Brand> Brands { get; }
    DbSet<BusinessBrand> BusinessBrands { get; }
    DbSet<BusinessFact> Facts { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerContact> CustomerContacts { get; }
    DbSet<PlatformConnection> Connections { get; }
    DbSet<Scan> Scans { get; }
    DbSet<Finding> Findings { get; }
    DbSet<FindingEvidence> FindingEvidence { get; }
    DbSet<WebsiteSnapshot> WebsiteSnapshots { get; }
    DbSet<SearchObservation> SearchObservations { get; }
    DbSet<SocialContentItem> SocialContent { get; }
    DbSet<SocialMetricSnapshot> SocialMetrics { get; }
    DbSet<DirectoryTask> DirectoryTasks { get; }
    DbSet<DirectoryStep> DirectorySteps { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectServiceLink> ProjectServices { get; }
    DbSet<ProjectBrandLink> ProjectBrands { get; }
    DbSet<MediaAsset> MediaAssets { get; }
    DbSet<ProjectMedia> ProjectMedia { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<ContentVariant> ContentVariants { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalDecision> ApprovalDecisions { get; }
    DbSet<KnowledgeEntry> KnowledgeEntries { get; }
    DbSet<GraphNode> GraphNodes { get; }
    DbSet<GraphEdge> GraphEdges { get; }
    DbSet<AiRun> AiRuns { get; }
    DbSet<AiEvaluation> AiEvaluations { get; }
    DbSet<AiAuditEvent> AiAuditEvents { get; }
    DbSet<AutomationPolicy> AutomationPolicies { get; }
    DbSet<WorkAction> WorkActions { get; }
    DbSet<ActionAttempt> ActionAttempts { get; }
    DbSet<ActionVerification> ActionVerifications { get; }
    DbSet<WhatsAppAccount> WhatsAppAccounts { get; }
    DbSet<WhatsAppContact> WhatsAppContacts { get; }
    DbSet<WhatsAppTemplate> WhatsAppTemplates { get; }
    DbSet<WhatsAppCampaign> WhatsAppCampaigns { get; }
    DbSet<WhatsAppConversation> WhatsAppConversations { get; }
    DbSet<WhatsAppMessage> WhatsAppMessages { get; }
    DbSet<WhatsAppMessageAttempt> WhatsAppMessageAttempts { get; }
    DbSet<WhatsAppWebhookEvent> WhatsAppWebhookEvents { get; }
    DbSet<MonitoringSchedule> MonitoringSchedules { get; }
    DbSet<MonitoringRun> MonitoringRuns { get; }
    DbSet<MonitoringResult> MonitoringResults { get; }
    DbSet<MonitoringAlert> MonitoringAlerts { get; }
    DbSet<Competitor> Competitors { get; }
    DbSet<CompetitorObservation> CompetitorObservations { get; }
    DbSet<PresenceReport> PresenceReports { get; }

    Task<PlatformConnection?> FindConnectionByStateAsync(string state, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
