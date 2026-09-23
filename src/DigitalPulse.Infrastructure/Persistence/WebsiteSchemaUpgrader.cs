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
    }
}
