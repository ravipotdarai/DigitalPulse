using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class WebsiteSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.WebsiteSnapshot', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[WebsiteSnapshot] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Url] nvarchar(2048) NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [StatusCode] int NULL,
                    [Title] nvarchar(240) NULL,
                    [MetaDescription] nvarchar(400) NULL,
                    [H1] nvarchar(240) NULL,
                    [CanonicalUrl] nvarchar(2048) NULL,
                    [Robots] nvarchar(160) NULL,
                    [HasJsonLd] bit NOT NULL,
                    [HasFaqSchema] bit NOT NULL,
                    [HasOrganizationSchema] bit NOT NULL,
                    [HasOgTitle] bit NOT NULL,
                    [WordCount] int NOT NULL,
                    [ContainsBusinessName] bit NOT NULL,
                    [ContainsPhone] bit NOT NULL,
                    [Error] nvarchar(500) NULL,
                    [FetchedAtUtc] datetimeoffset NOT NULL,
                    CONSTRAINT [PK_WebsiteSnapshot] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_WebsiteSnapshot_BusinessId_FetchedAtUtc] ON [dp].[WebsiteSnapshot] ([BusinessId], [FetchedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.SearchObservation', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[SearchObservation] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [SnapshotId] uniqueidentifier NOT NULL,
                    [Category] nvarchar(32) NOT NULL,
                    [Severity] nvarchar(32) NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Detail] nvarchar(1000) NOT NULL,
                    [ExpectedValue] nvarchar(400) NULL,
                    [ObservedValue] nvarchar(400) NULL,
                    [Recommendation] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_SearchObservation] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_SearchObservation_SnapshotId] ON [dp].[SearchObservation] ([SnapshotId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.WebsiteSnapshot', 'PageRole') IS NULL
                ALTER TABLE [dp].[WebsiteSnapshot] ADD [PageRole] nvarchar(32) NOT NULL CONSTRAINT [DF_WebsiteSnapshot_PageRole] DEFAULT 'Home';
            IF COL_LENGTH('dp.WebsiteSnapshot', 'AuditRunId') IS NULL
                ALTER TABLE [dp].[WebsiteSnapshot] ADD [AuditRunId] uniqueidentifier NULL;
            IF COL_LENGTH('dp.WebsiteSnapshot', 'ContainsEmail') IS NULL
                ALTER TABLE [dp].[WebsiteSnapshot] ADD [ContainsEmail] bit NOT NULL CONSTRAINT [DF_WebsiteSnapshot_ContainsEmail] DEFAULT 0;
            IF COL_LENGTH('dp.WebsiteSnapshot', 'ContainsAddress') IS NULL
                ALTER TABLE [dp].[WebsiteSnapshot] ADD [ContainsAddress] bit NOT NULL CONSTRAINT [DF_WebsiteSnapshot_ContainsAddress] DEFAULT 0;
            IF COL_LENGTH('dp.WebsiteSnapshot', 'ContainsVision') IS NULL
                ALTER TABLE [dp].[WebsiteSnapshot] ADD [ContainsVision] bit NOT NULL CONSTRAINT [DF_WebsiteSnapshot_ContainsVision] DEFAULT 0;
            IF COL_LENGTH('dp.WebsiteSnapshot', 'HasContactForm') IS NULL
                ALTER TABLE [dp].[WebsiteSnapshot] ADD [HasContactForm] bit NOT NULL CONSTRAINT [DF_WebsiteSnapshot_HasContactForm] DEFAULT 0;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.SearchConsoleQuery', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[SearchConsoleQuery] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [AuditRunId] uniqueidentifier NOT NULL,
                    [Query] nvarchar(400) NOT NULL,
                    [Clicks] float NOT NULL,
                    [Impressions] float NOT NULL,
                    [Ctr] float NOT NULL,
                    [Position] float NOT NULL,
                    CONSTRAINT [PK_SearchConsoleQuery] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_SearchConsoleQuery_BusinessId_AuditRunId] ON [dp].[SearchConsoleQuery] ([BusinessId], [AuditRunId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.TestReport', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[TestReport] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [ObservedFact] nvarchar(2000) NOT NULL,
                    [Recommendation] nvarchar(1000) NOT NULL,
                    [HoldReason] nvarchar(1000) NOT NULL,
                    [Body] nvarchar(max) NOT NULL,
                    [AuditRunId] uniqueidentifier NULL,
                    CONSTRAINT [PK_TestReport] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_TestReport_BusinessId_CreatedAtUtc] ON [dp].[TestReport] ([BusinessId], [CreatedAtUtc]);
            END
            """, cancellationToken);
    }
}
