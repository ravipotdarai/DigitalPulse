using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class OperationsSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await CreateAsync(db, "BackupSnapshot", """
            CREATE TABLE [dp].[BackupSnapshot] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Manifest] nvarchar(2000) NOT NULL,
                [Checksum] nvarchar(160) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_BackupSnapshot] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_BackupSnapshot_TenantId_CreatedAtUtc] ON [dp].[BackupSnapshot] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "RestoreAttempt", """
            CREATE TABLE [dp].[RestoreAttempt] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [SnapshotId] uniqueidentifier NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_RestoreAttempt] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_RestoreAttempt_TenantId_CreatedAtUtc] ON [dp].[RestoreAttempt] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "DisasterDrill", """
            CREATE TABLE [dp].[DisasterDrill] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(16) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [ObservedFact] nvarchar(500) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_DisasterDrill] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_DisasterDrill_TenantId_CreatedAtUtc] ON [dp].[DisasterDrill] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "DependencyInventory", """
            CREATE TABLE [dp].[DependencyInventory] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(16) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Packages] nvarchar(2000) NOT NULL,
                [PackageCount] int NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_DependencyInventory] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_DependencyInventory_TenantId_CreatedAtUtc] ON [dp].[DependencyInventory] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "ReadinessReview", """
            CREATE TABLE [dp].[ReadinessReview] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Status] nvarchar(24) NOT NULL,
                [HoldCount] int NOT NULL,
                [FailCount] int NOT NULL,
                [EnvironmentName] nvarchar(32) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_ReadinessReview] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_ReadinessReview_TenantId_CreatedAtUtc] ON [dp].[ReadinessReview] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "ReadinessCheck", """
            CREATE TABLE [dp].[ReadinessCheck] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [ReviewId] uniqueidentifier NOT NULL,
                [Code] nvarchar(40) NOT NULL,
                [Title] nvarchar(160) NOT NULL,
                [Outcome] nvarchar(8) NOT NULL,
                [Detail] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_ReadinessCheck] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_ReadinessCheck_ReviewId] ON [dp].[ReadinessCheck] ([ReviewId]);
            """, cancellationToken);

        await CreateAsync(db, "OperationsAudit", """
            CREATE TABLE [dp].[OperationsAudit] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Action] nvarchar(40) NOT NULL,
                [Detail] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_OperationsAudit] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_OperationsAudit_TenantId_CreatedAtUtc] ON [dp].[OperationsAudit] ([TenantId], [CreatedAtUtc]);
            """, cancellationToken);
    }

    private static Task CreateAsync(AppDbContext db, string table, string sql, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            string.Concat("IF OBJECT_ID(N'dp.", table, "', N'U') IS NULL BEGIN ", sql, " END"),
            cancellationToken);
}
