using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class AgencySchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await CreateAsync(db, "AgencyClient", """
            CREATE TABLE [dp].[AgencyClient] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [ContactName] nvarchar(160) NULL,
                [ContactEmail] nvarchar(256) NULL,
                [Notes] nvarchar(500) NULL,
                [ExternalRef] nvarchar(80) NULL,
                CONSTRAINT [PK_AgencyClient] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_AgencyClient_TenantId_BusinessId] ON [dp].[AgencyClient] ([TenantId], [BusinessId]);
            """, cancellationToken);

        await CreateAsync(db, "WhiteLabelProfile", """
            CREATE TABLE [dp].[WhiteLabelProfile] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [DisplayName] nvarchar(160) NOT NULL,
                [SupportEmail] nvarchar(256) NULL,
                [SupportPhone] nvarchar(40) NULL,
                [PrimaryColor] nvarchar(7) NOT NULL,
                [LogoUrl] nvarchar(2048) NULL,
                [CustomDomain] nvarchar(253) NULL,
                [Enabled] bit NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_WhiteLabelProfile] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_WhiteLabelProfile_TenantId] ON [dp].[WhiteLabelProfile] ([TenantId]);
            """, cancellationToken);

        await CreateAsync(db, "AgencyWorkflow", """
            CREATE TABLE [dp].[AgencyWorkflow] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ClientId] uniqueidentifier NULL,
                [BusinessId] uniqueidentifier NULL,
                [Kind] nvarchar(32) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [CurrentStep] int NOT NULL,
                [CurrentStepName] nvarchar(160) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_AgencyWorkflow] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_AgencyWorkflow_TenantId_UpdatedAtUtc] ON [dp].[AgencyWorkflow] ([TenantId], [UpdatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "AgencyWorkflowStep", """
            CREATE TABLE [dp].[AgencyWorkflowStep] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [WorkflowId] uniqueidentifier NOT NULL,
                [Ordinal] int NOT NULL,
                [Name] nvarchar(160) NOT NULL,
                [Completed] bit NOT NULL,
                [Note] nvarchar(500) NULL,
                CONSTRAINT [PK_AgencyWorkflowStep] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_AgencyWorkflowStep_WorkflowId_Ordinal] ON [dp].[AgencyWorkflowStep] ([WorkflowId], [Ordinal]);
            """, cancellationToken);

        await CreateAsync(db, "AgencyReport", """
            CREATE TABLE [dp].[AgencyReport] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Scope] nvarchar(16) NOT NULL,
                [ClientId] uniqueidentifier NULL,
                [BusinessId] uniqueidentifier NULL,
                [Title] nvarchar(200) NOT NULL,
                [ObservedFact] nvarchar(2000) NOT NULL,
                [Recommendation] nvarchar(2000) NOT NULL,
                [AiInterpretation] nvarchar(2000) NOT NULL,
                [CustomerDecision] nvarchar(500) NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_AgencyReport] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_AgencyReport_TenantId_CreatedAtUtc] ON [dp].[AgencyReport] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "AgencyReportLine", """
            CREATE TABLE [dp].[AgencyReportLine] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ReportId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NULL,
                [Kind] nvarchar(24) NOT NULL,
                [Body] nvarchar(2000) NOT NULL,
                CONSTRAINT [PK_AgencyReportLine] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_AgencyReportLine_ReportId] ON [dp].[AgencyReportLine] ([ReportId]);
            """, cancellationToken);
    }

    private static Task CreateAsync(AppDbContext db, string table, string sql, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            string.Concat("IF OBJECT_ID(N'dp.", table, "', N'U') IS NULL BEGIN ", sql, " END"),
            cancellationToken);
}
