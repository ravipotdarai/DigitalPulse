using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class AiSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.KnowledgeEntry', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[KnowledgeEntry] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Body] nvarchar(4000) NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [SourceUrl] nvarchar(2048) NULL,
                    CONSTRAINT [PK_KnowledgeEntry] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_KnowledgeEntry_BusinessId_CreatedAtUtc] ON [dp].[KnowledgeEntry] ([BusinessId], [CreatedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.GraphNode', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[GraphNode] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Label] nvarchar(160) NOT NULL,
                    [SourceKey] nvarchar(160) NOT NULL,
                    [Value] nvarchar(2000) NULL,
                    CONSTRAINT [PK_GraphNode] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_GraphNode_BusinessId_SourceKey] ON [dp].[GraphNode] ([BusinessId], [SourceKey]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.GraphEdge', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[GraphEdge] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [FromNodeId] uniqueidentifier NOT NULL,
                    [ToNodeId] uniqueidentifier NOT NULL,
                    [Relation] nvarchar(80) NOT NULL,
                    CONSTRAINT [PK_GraphEdge] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_GraphEdge_BusinessId_FromNodeId_ToNodeId_Relation]
                    ON [dp].[GraphEdge] ([BusinessId], [FromNodeId], [ToNodeId], [Relation]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.AiRun', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[AiRun] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Agent] nvarchar(32) NOT NULL,
                    [Prompt] nvarchar(4000) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Confidence] nvarchar(32) NOT NULL,
                    [Output] nvarchar(4000) NOT NULL,
                    [ProviderName] nvarchar(80) NOT NULL,
                    [ProviderIsLive] bit NOT NULL,
                    [HoldReason] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_AiRun] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_AiRun_BusinessId_CreatedAtUtc] ON [dp].[AiRun] ([BusinessId], [CreatedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.AiEvaluation', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[AiEvaluation] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [AiRunId] uniqueidentifier NOT NULL,
                    [Passed] bit NOT NULL,
                    [HasEvidence] bit NOT NULL,
                    [HasConflict] bit NOT NULL,
                    [HasRestrictedFact] bit NOT NULL,
                    [Confidence] nvarchar(32) NOT NULL,
                    [Summary] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_AiEvaluation] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_AiEvaluation_AiRunId] ON [dp].[AiEvaluation] ([AiRunId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.AiAuditEvent', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[AiAuditEvent] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [AiRunId] uniqueidentifier NOT NULL,
                    [Stage] nvarchar(40) NOT NULL,
                    [Detail] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_AiAuditEvent] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_AiAuditEvent_AiRunId_CreatedAtUtc] ON [dp].[AiAuditEvent] ([AiRunId], [CreatedAtUtc]);
            END
            """, cancellationToken);
    }
}
