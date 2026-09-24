using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Actions;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Common;
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

namespace DigitalPulse.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    private readonly bool _bypassTenantFilter;
    private readonly Guid? _filterTenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _bypassTenantFilter = tenantContext is null;
        _filterTenantId = tenantContext?.TenantId;
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> Memberships => Set<TenantMembership>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessLocation> Locations => Set<BusinessLocation>();
    public DbSet<SubscriptionPlan> Plans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<FactType> FactTypes => Set<FactType>();
    public DbSet<ContactPoint> ContactPoints => Set<ContactPoint>();
    public DbSet<BusinessCategory> Categories => Set<BusinessCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BusinessBrand> BusinessBrands => Set<BusinessBrand>();
    public DbSet<BusinessFact> Facts => Set<BusinessFact>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<PlatformConnection> Connections => Set<PlatformConnection>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<FindingEvidence> FindingEvidence => Set<FindingEvidence>();
    public DbSet<WebsiteSnapshot> WebsiteSnapshots => Set<WebsiteSnapshot>();
    public DbSet<SearchObservation> SearchObservations => Set<SearchObservation>();
    public DbSet<SocialContentItem> SocialContent => Set<SocialContentItem>();
    public DbSet<SocialMetricSnapshot> SocialMetrics => Set<SocialMetricSnapshot>();
    public DbSet<DirectoryTask> DirectoryTasks => Set<DirectoryTask>();
    public DbSet<DirectoryStep> DirectorySteps => Set<DirectoryStep>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectServiceLink> ProjectServices => Set<ProjectServiceLink>();
    public DbSet<ProjectBrandLink> ProjectBrands => Set<ProjectBrandLink>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ProjectMedia> ProjectMedia => Set<ProjectMedia>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentVariant> ContentVariants => Set<ContentVariant>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<KnowledgeEntry> KnowledgeEntries => Set<KnowledgeEntry>();
    public DbSet<GraphNode> GraphNodes => Set<GraphNode>();
    public DbSet<GraphEdge> GraphEdges => Set<GraphEdge>();
    public DbSet<AiRun> AiRuns => Set<AiRun>();
    public DbSet<AiEvaluation> AiEvaluations => Set<AiEvaluation>();
    public DbSet<AiAuditEvent> AiAuditEvents => Set<AiAuditEvent>();
    public DbSet<AutomationPolicy> AutomationPolicies => Set<AutomationPolicy>();
    public DbSet<WorkAction> WorkActions => Set<WorkAction>();
    public DbSet<ActionAttempt> ActionAttempts => Set<ActionAttempt>();
    public DbSet<ActionVerification> ActionVerifications => Set<ActionVerification>();
    public DbSet<WhatsAppAccount> WhatsAppAccounts => Set<WhatsAppAccount>();
    public DbSet<WhatsAppContact> WhatsAppContacts => Set<WhatsAppContact>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<WhatsAppCampaign> WhatsAppCampaigns => Set<WhatsAppCampaign>();
    public DbSet<WhatsAppConversation> WhatsAppConversations => Set<WhatsAppConversation>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();
    public DbSet<WhatsAppMessageAttempt> WhatsAppMessageAttempts => Set<WhatsAppMessageAttempt>();
    public DbSet<WhatsAppWebhookEvent> WhatsAppWebhookEvents => Set<WhatsAppWebhookEvent>();
    public DbSet<MonitoringSchedule> MonitoringSchedules => Set<MonitoringSchedule>();
    public DbSet<MonitoringRun> MonitoringRuns => Set<MonitoringRun>();
    public DbSet<MonitoringResult> MonitoringResults => Set<MonitoringResult>();
    public DbSet<MonitoringAlert> MonitoringAlerts => Set<MonitoringAlert>();
    public DbSet<Competitor> Competitors => Set<Competitor>();
    public DbSet<CompetitorObservation> CompetitorObservations => Set<CompetitorObservation>();
    public DbSet<PresenceReport> PresenceReports => Set<PresenceReport>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<BillingWebhookEvent> BillingWebhookEvents => Set<BillingWebhookEvent>();

    public Task<PlatformConnection?> FindConnectionByStateAsync(string state, CancellationToken cancellationToken) =>
        Connections.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.AuthorizationState == state, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dp");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantFilter<TenantMembership>(modelBuilder);
        ApplyTenantFilter<Business>(modelBuilder);
        ApplyTenantFilter<BusinessLocation>(modelBuilder);
        ApplyTenantFilter<Subscription>(modelBuilder);
        ApplyTenantFilter<ContactPoint>(modelBuilder);
        ApplyTenantFilter<BusinessCategory>(modelBuilder);
        ApplyTenantFilter<Service>(modelBuilder);
        ApplyTenantFilter<Brand>(modelBuilder);
        ApplyTenantFilter<BusinessBrand>(modelBuilder);
        ApplyTenantFilter<BusinessFact>(modelBuilder);
        ApplyTenantFilter<Customer>(modelBuilder);
        ApplyTenantFilter<CustomerContact>(modelBuilder);
        ApplyTenantFilter<PlatformConnection>(modelBuilder);
        ApplyTenantFilter<Scan>(modelBuilder);
        ApplyTenantFilter<Finding>(modelBuilder);
        ApplyTenantFilter<FindingEvidence>(modelBuilder);
        ApplyTenantFilter<WebsiteSnapshot>(modelBuilder);
        ApplyTenantFilter<SearchObservation>(modelBuilder);
        ApplyTenantFilter<SocialContentItem>(modelBuilder);
        ApplyTenantFilter<SocialMetricSnapshot>(modelBuilder);
        ApplyTenantFilter<DirectoryTask>(modelBuilder);
        ApplyTenantFilter<DirectoryStep>(modelBuilder);
        ApplyTenantFilter<Project>(modelBuilder);
        ApplyTenantFilter<ProjectServiceLink>(modelBuilder);
        ApplyTenantFilter<ProjectBrandLink>(modelBuilder);
        ApplyTenantFilter<MediaAsset>(modelBuilder);
        ApplyTenantFilter<ProjectMedia>(modelBuilder);
        ApplyTenantFilter<ContentItem>(modelBuilder);
        ApplyTenantFilter<ContentVariant>(modelBuilder);
        ApplyTenantFilter<ApprovalRequest>(modelBuilder);
        ApplyTenantFilter<ApprovalDecision>(modelBuilder);
        ApplyTenantFilter<KnowledgeEntry>(modelBuilder);
        ApplyTenantFilter<GraphNode>(modelBuilder);
        ApplyTenantFilter<GraphEdge>(modelBuilder);
        ApplyTenantFilter<AiRun>(modelBuilder);
        ApplyTenantFilter<AiEvaluation>(modelBuilder);
        ApplyTenantFilter<AiAuditEvent>(modelBuilder);
        ApplyTenantFilter<AutomationPolicy>(modelBuilder);
        ApplyTenantFilter<WorkAction>(modelBuilder);
        ApplyTenantFilter<ActionAttempt>(modelBuilder);
        ApplyTenantFilter<ActionVerification>(modelBuilder);
        ApplyTenantFilter<WhatsAppAccount>(modelBuilder);
        ApplyTenantFilter<WhatsAppContact>(modelBuilder);
        ApplyTenantFilter<WhatsAppTemplate>(modelBuilder);
        ApplyTenantFilter<WhatsAppCampaign>(modelBuilder);
        ApplyTenantFilter<WhatsAppConversation>(modelBuilder);
        ApplyTenantFilter<WhatsAppMessage>(modelBuilder);
        ApplyTenantFilter<WhatsAppMessageAttempt>(modelBuilder);
        ApplyTenantFilter<WhatsAppWebhookEvent>(modelBuilder);
        ApplyTenantFilter<MonitoringSchedule>(modelBuilder);
        ApplyTenantFilter<MonitoringRun>(modelBuilder);
        ApplyTenantFilter<MonitoringResult>(modelBuilder);
        ApplyTenantFilter<MonitoringAlert>(modelBuilder);
        ApplyTenantFilter<Competitor>(modelBuilder);
        ApplyTenantFilter<CompetitorObservation>(modelBuilder);
        ApplyTenantFilter<PresenceReport>(modelBuilder);
        ApplyTenantFilter<Invoice>(modelBuilder);
        ApplyTenantFilter<InvoiceLine>(modelBuilder);
        ApplyTenantFilter<PaymentAttempt>(modelBuilder);
        ApplyTenantFilter<UsageRecord>(modelBuilder);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantOwnedEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _bypassTenantFilter || (_filterTenantId != null && e.TenantId == _filterTenantId));

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Touch();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
