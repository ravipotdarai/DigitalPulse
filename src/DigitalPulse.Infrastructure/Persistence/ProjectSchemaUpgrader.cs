using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class ProjectSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.Project', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[Project] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    [ClientName] nvarchar(160) NULL,
                    [Industry] nvarchar(80) NULL,
                    [Location] nvarchar(160) NULL,
                    [Description] nvarchar(4000) NULL,
                    [Outcomes] nvarchar(2000) NULL,
                    [StartedOn] date NULL,
                    [CompletedOn] date NULL,
                    [PermissionScope] nvarchar(32) NOT NULL,
                    [Confidentiality] nvarchar(32) NOT NULL,
                    [PublicationStatus] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK_Project] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_Project_BusinessId_UpdatedAtUtc] ON [dp].[Project] ([BusinessId], [UpdatedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ProjectService', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ProjectService] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ProjectId] uniqueidentifier NOT NULL,
                    [ServiceId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_ProjectService] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ProjectService_ProjectId_ServiceId] ON [dp].[ProjectService] ([ProjectId], [ServiceId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ProjectBrand', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ProjectBrand] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ProjectId] uniqueidentifier NOT NULL,
                    [BrandId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_ProjectBrand] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ProjectBrand_ProjectId_BrandId] ON [dp].[ProjectBrand] ([ProjectId], [BrandId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.MediaAsset', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[MediaAsset] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Label] nvarchar(160) NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [SourceUrl] nvarchar(2048) NULL,
                    [Note] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_MediaAsset] PRIMARY KEY ([Id])
                );
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ProjectMedia', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ProjectMedia] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ProjectId] uniqueidentifier NOT NULL,
                    [MediaAssetId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_ProjectMedia] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ProjectMedia_ProjectId_MediaAssetId] ON [dp].[ProjectMedia] ([ProjectId], [MediaAssetId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ContentItem', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ContentItem] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [ProjectId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [SourceNote] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_ContentItem] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_ContentItem_ProjectId_UpdatedAtUtc] ON [dp].[ContentItem] ([ProjectId], [UpdatedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ContentVariant', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ContentVariant] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ContentItemId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(40) NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Body] nvarchar(4000) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [PublicationHold] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_ContentVariant] PRIMARY KEY ([Id])
                );
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ApprovalRequest', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ApprovalRequest] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ContentItemId] uniqueidentifier NOT NULL,
                    [Reason] nvarchar(500) NOT NULL,
                    [Open] bit NOT NULL,
                    CONSTRAINT [PK_ApprovalRequest] PRIMARY KEY ([Id])
                );
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ApprovalDecision', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ApprovalDecision] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [ApprovalRequestId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Note] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_ApprovalDecision] PRIMARY KEY ([Id])
                );
            END
            """, cancellationToken);
    }
}
