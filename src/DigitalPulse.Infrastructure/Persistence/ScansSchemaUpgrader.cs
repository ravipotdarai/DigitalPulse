using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class ScansSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.SubscriptionPlan', 'ScansPerMonth') IS NULL
                ALTER TABLE [dp].[SubscriptionPlan] ADD [ScansPerMonth] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_ScansPerMonth] DEFAULT 2;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [dp].[SubscriptionPlan] SET [ScansPerMonth] = CASE [Code]
                WHEN 'STARTER' THEN 2
                WHEN 'GROWTH' THEN 10
                WHEN 'BUSINESS' THEN 30
                WHEN 'AGENCY' THEN 200
                ELSE [ScansPerMonth] END;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.Scan', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[Scan] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Trigger] nvarchar(32) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [StartedAtUtc] datetimeoffset NOT NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    [Summary] nvarchar(240) NULL,
                    [Error] nvarchar(500) NULL,
                    CONSTRAINT [PK_Scan] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_Scan_TenantId_StartedAtUtc] ON [dp].[Scan] ([TenantId], [StartedAtUtc]);
                CREATE INDEX [IX_Scan_BusinessId_StartedAtUtc] ON [dp].[Scan] ([BusinessId], [StartedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.Finding', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[Finding] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ScanId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Category] nvarchar(32) NOT NULL,
                    [Severity] nvarchar(32) NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Description] nvarchar(1000) NOT NULL,
                    [ExpectedValue] nvarchar(400) NULL,
                    [ObservedValue] nvarchar(400) NULL,
                    [Recommendation] nvarchar(500) NOT NULL,
                    [SuggestedAction] nvarchar(500) NOT NULL,
                    [VerificationMethod] nvarchar(400) NOT NULL,
                    [AutomationState] nvarchar(32) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK_Finding] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_Finding_ScanId] ON [dp].[Finding] ([ScanId]);
                CREATE INDEX [IX_Finding_BusinessId_Status] ON [dp].[Finding] ([BusinessId], [Status]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.FindingEvidence', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[FindingEvidence] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [FindingId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Label] nvarchar(120) NOT NULL,
                    [Value] nvarchar(500) NOT NULL,
                    [Source] nvarchar(120) NOT NULL,
                    CONSTRAINT [PK_FindingEvidence] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_FindingEvidence_FindingId] ON [dp].[FindingEvidence] ([FindingId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.Finding', 'ResolutionPath') IS NULL
                ALTER TABLE [dp].[Finding] ADD [ResolutionPath] nvarchar(32) NOT NULL CONSTRAINT [DF_Finding_ResolutionPath] DEFAULT 'AssistedPlaybook';
            IF COL_LENGTH('dp.Finding', 'PlaybookCode') IS NULL
                ALTER TABLE [dp].[Finding] ADD [PlaybookCode] nvarchar(64) NULL;
            IF COL_LENGTH('dp.Finding', 'VerifiedAtUtc') IS NULL
                ALTER TABLE [dp].[Finding] ADD [VerifiedAtUtc] datetimeoffset NULL;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.FindingStep', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[FindingStep] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [FindingId] uniqueidentifier NOT NULL,
                    [Ordinal] int NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Detail] nvarchar(1000) NOT NULL,
                    [OfficialUrl] nvarchar(2048) NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    CONSTRAINT [PK_FindingStep] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_FindingStep_FindingId] ON [dp].[FindingStep] ([FindingId]);
            END
            """, cancellationToken);
    }
}
