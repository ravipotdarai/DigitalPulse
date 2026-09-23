using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class SocialSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.SocialContentItem', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[SocialContentItem] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [PlatformCode] nvarchar(32) NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Body] nvarchar(4000) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [VerificationStatus] nvarchar(32) NOT NULL,
                    [VerificationDetail] nvarchar(500) NULL,
                    [LastPublishError] nvarchar(500) NULL,
                    CONSTRAINT [PK_SocialContentItem] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_SocialContentItem_BusinessId_UpdatedAtUtc] ON [dp].[SocialContentItem] ([BusinessId], [UpdatedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.SocialMetricSnapshot', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[SocialMetricSnapshot] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [ConnectionId] uniqueidentifier NULL,
                    [PlatformCode] nvarchar(32) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Detail] nvarchar(500) NOT NULL,
                    [CapturedAtUtc] datetimeoffset NOT NULL,
                    CONSTRAINT [PK_SocialMetricSnapshot] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_SocialMetricSnapshot_BusinessId_CapturedAtUtc] ON [dp].[SocialMetricSnapshot] ([BusinessId], [CapturedAtUtc]);
            END
            """, cancellationToken);
    }
}
