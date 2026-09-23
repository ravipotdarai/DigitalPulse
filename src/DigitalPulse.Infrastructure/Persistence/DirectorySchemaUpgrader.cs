using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class DirectorySchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.DirectoryTask', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[DirectoryTask] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [PlatformCode] nvarchar(32) NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [PreparedName] nvarchar(160) NOT NULL,
                    [PreparedPhone] nvarchar(64) NULL,
                    [PreparedWebsite] nvarchar(2048) NULL,
                    [PreparedCategory] nvarchar(160) NULL,
                    [PreparedServices] nvarchar(1000) NULL,
                    [VerificationNote] nvarchar(500) NULL,
                    [VerifiedAtUtc] datetimeoffset NULL,
                    [LastMonitoredAtUtc] datetimeoffset NULL,
                    [MonitorDetail] nvarchar(500) NULL,
                    CONSTRAINT [PK_DirectoryTask] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_DirectoryTask_BusinessId_UpdatedAtUtc] ON [dp].[DirectoryTask] ([BusinessId], [UpdatedAtUtc]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.DirectoryStep', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[DirectoryStep] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [TaskId] uniqueidentifier NOT NULL,
                    [Ordinal] int NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [Detail] nvarchar(500) NOT NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    CONSTRAINT [PK_DirectoryStep] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_DirectoryStep_TaskId] ON [dp].[DirectoryStep] ([TaskId]);
            END
            """, cancellationToken);
    }
}
