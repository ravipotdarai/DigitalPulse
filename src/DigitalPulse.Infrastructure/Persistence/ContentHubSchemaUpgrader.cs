using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class ContentHubSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.ContentItem', 'ContentTypeCode') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [ContentTypeCode] nvarchar(32) NOT NULL CONSTRAINT [DF_ContentItem_Type] DEFAULT ('PROJECT_STORY');
            IF COL_LENGTH('dp.ContentItem', 'AuthorUserId') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [AuthorUserId] uniqueidentifier NULL;
            IF COL_LENGTH('dp.ContentItem', 'Slug') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [Slug] nvarchar(80) NULL;
            IF COL_LENGTH('dp.ContentItem', 'Excerpt') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [Excerpt] nvarchar(500) NOT NULL CONSTRAINT [DF_ContentItem_Excerpt] DEFAULT ('');
            IF COL_LENGTH('dp.ContentItem', 'Body') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [Body] nvarchar(max) NOT NULL CONSTRAINT [DF_ContentItem_Body] DEFAULT ('');
            IF COL_LENGTH('dp.ContentItem', 'Visibility') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [Visibility] nvarchar(16) NOT NULL CONSTRAINT [DF_ContentItem_Visibility] DEFAULT ('Private');
            IF COL_LENGTH('dp.ContentItem', 'FeaturedMediaAssetId') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [FeaturedMediaAssetId] uniqueidentifier NULL;
            IF COL_LENGTH('dp.ContentItem', 'CanonicalUrl') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [CanonicalUrl] nvarchar(2048) NULL;
            IF COL_LENGTH('dp.ContentItem', 'PublishedAtUtc') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [PublishedAtUtc] datetimeoffset NULL;
            IF COL_LENGTH('dp.ContentItem', 'ScheduledAtUtc') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [ScheduledAtUtc] datetimeoffset NULL;
            IF COL_LENGTH('dp.ContentItem', 'ContentTypeId') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [ContentTypeId] uniqueidentifier NULL;
            IF COL_LENGTH('dp.ContentItem', 'RowVersion') IS NULL
                ALTER TABLE [dp].[ContentItem] ADD [RowVersion] rowversion NOT NULL;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.ContentItem', 'Slug') IS NOT NULL
               AND EXISTS (SELECT 1 FROM [dp].[ContentItem] WHERE [Slug] IS NULL)
            BEGIN
                UPDATE [dp].[ContentItem] SET [Slug] = LOWER(CONCAT('pack-', CONVERT(varchar(36), [Id]))) WHERE [Slug] IS NULL;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.ContentItem', 'Slug') IS NOT NULL
               AND COLUMNPROPERTY(OBJECT_ID(N'dp.ContentItem'), 'Slug', 'AllowsNull') = 1
                ALTER TABLE [dp].[ContentItem] ALTER COLUMN [Slug] nvarchar(80) NOT NULL;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            DECLARE @fk sysname;
            SELECT @fk = fk.name
            FROM sys.foreign_keys fk
            INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = N'dp' AND t.name = N'ContentItem' AND fk.referenced_object_id = OBJECT_ID(N'dp.Project');
            IF @fk IS NOT NULL
                EXEC('ALTER TABLE [dp].[ContentItem] DROP CONSTRAINT [' + @fk + ']');
            ALTER TABLE [dp].[ContentItem] ALTER COLUMN [ProjectId] uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentItem_BusinessId_Slug' AND object_id = OBJECT_ID(N'dp.ContentItem'))
                CREATE UNIQUE INDEX [IX_ContentItem_BusinessId_Slug] ON [dp].[ContentItem] ([BusinessId], [Slug]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentItem_TenantId_BusinessId' AND object_id = OBJECT_ID(N'dp.ContentItem'))
                CREATE INDEX [IX_ContentItem_TenantId_BusinessId] ON [dp].[ContentItem] ([TenantId], [BusinessId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentItem_BusinessId_Status' AND object_id = OBJECT_ID(N'dp.ContentItem'))
                CREATE INDEX [IX_ContentItem_BusinessId_Status] ON [dp].[ContentItem] ([BusinessId], [Status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentItem_BusinessId_PublishedAtUtc' AND object_id = OBJECT_ID(N'dp.ContentItem'))
                CREATE INDEX [IX_ContentItem_BusinessId_PublishedAtUtc] ON [dp].[ContentItem] ([BusinessId], [PublishedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "ContentType",
            """
            CREATE TABLE [dp].[ContentType] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [Code] nvarchar(32) NOT NULL,
                [Name] nvarchar(80) NOT NULL,
                CONSTRAINT [PK_ContentType] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentType_Code] ON [dp].[ContentType] ([Code]);
            """, cancellationToken);

        await CreateAsync(db, "ContentCategory",
            """
            CREATE TABLE [dp].[ContentCategory] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Name] nvarchar(80) NOT NULL,
                [Slug] nvarchar(80) NOT NULL,
                [Description] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_ContentCategory] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentCategory_BusinessId_Slug] ON [dp].[ContentCategory] ([BusinessId], [Slug]);
            """, cancellationToken);

        await CreateAsync(db, "ContentTag",
            """
            CREATE TABLE [dp].[ContentTag] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Name] nvarchar(80) NOT NULL,
                [Slug] nvarchar(80) NOT NULL,
                CONSTRAINT [PK_ContentTag] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentTag_BusinessId_Slug] ON [dp].[ContentTag] ([BusinessId], [Slug]);
            """, cancellationToken);

        await CreateAsync(db, "ContentItemCategory",
            """
            CREATE TABLE [dp].[ContentItemCategory] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [ContentCategoryId] uniqueidentifier NOT NULL,
                CONSTRAINT [PK_ContentItemCategory] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentItemCategory_Item_Category] ON [dp].[ContentItemCategory] ([ContentItemId], [ContentCategoryId]);
            """, cancellationToken);

        await CreateAsync(db, "ContentItemTag",
            """
            CREATE TABLE [dp].[ContentItemTag] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [ContentTagId] uniqueidentifier NOT NULL,
                CONSTRAINT [PK_ContentItemTag] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentItemTag_Item_Tag] ON [dp].[ContentItemTag] ([ContentItemId], [ContentTagId]);
            """, cancellationToken);

        await CreateAsync(db, "ContentRevision",
            """
            CREATE TABLE [dp].[ContentRevision] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [VersionNumber] int NOT NULL,
                [Title] nvarchar(160) NOT NULL,
                [Excerpt] nvarchar(500) NOT NULL,
                [Body] nvarchar(max) NOT NULL,
                [ChangeSummary] nvarchar(500) NOT NULL,
                [CreatedByUserId] uniqueidentifier NULL,
                CONSTRAINT [PK_ContentRevision] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentRevision_Item_Version] ON [dp].[ContentRevision] ([ContentItemId], [VersionNumber]);
            """, cancellationToken);

        await CreateAsync(db, "ContentItemMedia",
            """
            CREATE TABLE [dp].[ContentItemMedia] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [MediaAssetId] uniqueidentifier NOT NULL,
                [DisplayOrder] int NOT NULL,
                [Role] nvarchar(24) NOT NULL,
                CONSTRAINT [PK_ContentItemMedia] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_ContentItemMedia_Item_Asset_Role] ON [dp].[ContentItemMedia] ([ContentItemId], [MediaAssetId], [Role]);
            """, cancellationToken);

        await CreateAsync(db, "ContentSeoAnalysis",
            """
            CREATE TABLE [dp].[ContentSeoAnalysis] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [FocusKeyword] nvarchar(80) NULL,
                [SearchIntent] nvarchar(32) NOT NULL,
                [ChecksPassed] int NOT NULL,
                [ChecksTotal] int NOT NULL,
                [SeoScore] int NOT NULL,
                [ReadabilityScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Readability] DEFAULT (0),
                [AeoScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Aeo] DEFAULT (0),
                [SlugScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Slug] DEFAULT (0),
                [InternalLinkScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Internal] DEFAULT (0),
                [EntityCoverageScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Entity] DEFAULT (0),
                [MetaTitle] nvarchar(80) NOT NULL,
                [MetaDescription] nvarchar(200) NOT NULL,
                [CanonicalUrl] nvarchar(2048) NULL,
                [NotesJson] nvarchar(max) NOT NULL,
                [LastAnalyzedAtUtc] datetimeoffset NOT NULL,
                CONSTRAINT [PK_ContentSeoAnalysis] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await CreateAsync(db, "ContentTopic",
            """
            CREATE TABLE [dp].[ContentTopic] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Topic] nvarchar(160) NOT NULL,
                [Description] nvarchar(1000) NOT NULL,
                [SearchIntent] nvarchar(32) NOT NULL,
                [Status] nvarchar(24) NOT NULL CONSTRAINT [DF_ContentTopic_Status] DEFAULT ('Open'),
                CONSTRAINT [PK_ContentTopic] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await CreateAsync(db, "ContentOpportunity",
            """
            CREATE TABLE [dp].[ContentOpportunity] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ContentTopicId] uniqueidentifier NOT NULL,
                [SourceType] nvarchar(32) NOT NULL,
                [RelevanceScore] int NULL,
                [OpportunityScore] int NULL,
                [CompetitionScore] int NULL,
                [CoverageScore] int NULL,
                [Priority] int NOT NULL CONSTRAINT [DF_ContentOpportunity_Priority] DEFAULT (5),
                [Reason] nvarchar(500) NOT NULL,
                [Status] nvarchar(24) NOT NULL,
                CONSTRAINT [PK_ContentOpportunity] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await CreateAsync(db, "ContentCalendarEntry",
            """
            CREATE TABLE [dp].[ContentCalendarEntry] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [ContentVariantId] uniqueidentifier NULL,
                [ScheduledAtUtc] datetimeoffset NOT NULL,
                [Status] nvarchar(24) NOT NULL,
                [Channel] nvarchar(32) NOT NULL,
                [CreatedByUserId] uniqueidentifier NULL,
                CONSTRAINT [PK_ContentCalendarEntry] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await CreateAsync(db, "ContentDistribution",
            """
            CREATE TABLE [dp].[ContentDistribution] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [ContentVariantId] uniqueidentifier NULL,
                [ProviderCode] nvarchar(32) NOT NULL,
                [PlatformConnectionId] uniqueidentifier NULL,
                [Status] nvarchar(32) NOT NULL,
                [ScheduledAtUtc] datetimeoffset NULL,
                [PublishedAtUtc] datetimeoffset NULL,
                [ExternalContentId] nvarchar(160) NULL,
                [FailureReason] nvarchar(500) NULL,
                CONSTRAINT [PK_ContentDistribution] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await CreateAsync(db, "ContentMetric",
            """
            CREATE TABLE [dp].[ContentMetric] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ContentItemId] uniqueidentifier NOT NULL,
                [ContentVariantId] uniqueidentifier NULL,
                [ProviderCode] nvarchar(32) NOT NULL,
                [MetricDate] date NOT NULL,
                [Views] bigint NULL,
                [Clicks] bigint NULL,
                [Engagements] bigint NULL,
                [Shares] bigint NULL,
                [Reactions] bigint NULL,
                [Comments] bigint NULL,
                [Leads] bigint NULL,
                [Conversions] bigint NULL,
                [Detail] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_ContentMetric] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ContentSeoAnalysis', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('dp.ContentSeoAnalysis', 'ReadabilityScore') IS NULL
                    ALTER TABLE [dp].[ContentSeoAnalysis] ADD [ReadabilityScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Readability] DEFAULT (0);
                IF COL_LENGTH('dp.ContentSeoAnalysis', 'AeoScore') IS NULL
                    ALTER TABLE [dp].[ContentSeoAnalysis] ADD [AeoScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Aeo] DEFAULT (0);
                IF COL_LENGTH('dp.ContentSeoAnalysis', 'SlugScore') IS NULL
                    ALTER TABLE [dp].[ContentSeoAnalysis] ADD [SlugScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Slug] DEFAULT (0);
                IF COL_LENGTH('dp.ContentSeoAnalysis', 'InternalLinkScore') IS NULL
                    ALTER TABLE [dp].[ContentSeoAnalysis] ADD [InternalLinkScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Internal] DEFAULT (0);
                IF COL_LENGTH('dp.ContentSeoAnalysis', 'EntityCoverageScore') IS NULL
                    ALTER TABLE [dp].[ContentSeoAnalysis] ADD [EntityCoverageScore] int NOT NULL CONSTRAINT [DF_ContentSeo_Entity] DEFAULT (0);
            END
            IF OBJECT_ID(N'dp.ContentTopic', N'U') IS NOT NULL AND COL_LENGTH('dp.ContentTopic', 'Status') IS NULL
                ALTER TABLE [dp].[ContentTopic] ADD [Status] nvarchar(24) NOT NULL CONSTRAINT [DF_ContentTopic_Status] DEFAULT ('Open');
            IF OBJECT_ID(N'dp.ContentOpportunity', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('dp.ContentOpportunity', 'RelevanceScore') IS NULL
                    ALTER TABLE [dp].[ContentOpportunity] ADD [RelevanceScore] int NULL;
                IF COL_LENGTH('dp.ContentOpportunity', 'CompetitionScore') IS NULL
                    ALTER TABLE [dp].[ContentOpportunity] ADD [CompetitionScore] int NULL;
                IF COL_LENGTH('dp.ContentOpportunity', 'Priority') IS NULL
                    ALTER TABLE [dp].[ContentOpportunity] ADD [Priority] int NOT NULL CONSTRAINT [DF_ContentOpportunity_Priority] DEFAULT (5);
            END
            IF OBJECT_ID(N'dp.ContentMetric', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('dp.ContentMetric', 'Clicks') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Clicks] bigint NULL;
                IF COL_LENGTH('dp.ContentMetric', 'Engagements') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Engagements] bigint NULL;
                IF COL_LENGTH('dp.ContentMetric', 'Shares') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Shares] bigint NULL;
                IF COL_LENGTH('dp.ContentMetric', 'Reactions') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Reactions] bigint NULL;
                IF COL_LENGTH('dp.ContentMetric', 'Comments') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Comments] bigint NULL;
                IF COL_LENGTH('dp.ContentMetric', 'Leads') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Leads] bigint NULL;
                IF COL_LENGTH('dp.ContentMetric', 'Conversions') IS NULL
                    ALTER TABLE [dp].[ContentMetric] ADD [Conversions] bigint NULL;
            END
            IF OBJECT_ID(N'dp.ContentItem', N'U') IS NOT NULL AND OBJECT_ID(N'dp.ContentType', N'U') IS NOT NULL
               AND COL_LENGTH('dp.ContentItem', 'ContentTypeId') IS NOT NULL
            BEGIN
                UPDATE ci SET ci.ContentTypeId = ct.Id
                FROM [dp].[ContentItem] ci
                INNER JOIN [dp].[ContentType] ct ON ct.Code = ci.ContentTypeCode
                WHERE ci.ContentTypeId IS NULL;
            END
            IF OBJECT_ID(N'dp.ContentItemMedia', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentItemMedia_Item_Asset_Role' AND object_id = OBJECT_ID(N'dp.ContentItemMedia'))
                CREATE UNIQUE INDEX [IX_ContentItemMedia_Item_Asset_Role] ON [dp].[ContentItemMedia] ([ContentItemId], [MediaAssetId], [Role]);
            IF OBJECT_ID(N'dp.ContentSeoAnalysis', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentSeoAnalysis_ContentItemId' AND object_id = OBJECT_ID(N'dp.ContentSeoAnalysis'))
                CREATE INDEX [IX_ContentSeoAnalysis_ContentItemId] ON [dp].[ContentSeoAnalysis] ([ContentItemId]);
            IF OBJECT_ID(N'dp.ContentOpportunity', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentOpportunity_BusinessId_Status' AND object_id = OBJECT_ID(N'dp.ContentOpportunity'))
                CREATE INDEX [IX_ContentOpportunity_BusinessId_Status] ON [dp].[ContentOpportunity] ([BusinessId], [Status]);
            IF OBJECT_ID(N'dp.ContentDistribution', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentDistribution_Item_Provider' AND object_id = OBJECT_ID(N'dp.ContentDistribution'))
                CREATE INDEX [IX_ContentDistribution_Item_Provider] ON [dp].[ContentDistribution] ([ContentItemId], [ProviderCode]);
            IF OBJECT_ID(N'dp.ContentMetric', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentMetric_Item_Provider_Date' AND object_id = OBJECT_ID(N'dp.ContentMetric'))
                CREATE INDEX [IX_ContentMetric_Item_Provider_Date] ON [dp].[ContentMetric] ([ContentItemId], [ProviderCode], [MetricDate]);
            """, cancellationToken);
    }

    private static Task CreateAsync(AppDbContext db, string table, string sql, CancellationToken cancellationToken)
    {
        if (!table.All(char.IsAsciiLetter))
        {
            throw new InvalidOperationException("Schema table names must be letters only.");
        }

        return db.Database.ExecuteSqlRawAsync(
            string.Concat("IF OBJECT_ID(N'dp.", table, "', N'U') IS NULL BEGIN ", sql, " END"),
            cancellationToken);
    }
}
