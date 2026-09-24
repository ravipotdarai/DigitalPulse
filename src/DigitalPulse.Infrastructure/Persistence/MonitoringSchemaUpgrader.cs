using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class MonitoringSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.SubscriptionPlan', 'MonitoringIntervalHours') IS NULL
                ALTER TABLE [dp].[SubscriptionPlan] ADD [MonitoringIntervalHours] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_MonitoringIntervalHours] DEFAULT 168;
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [dp].[SubscriptionPlan] SET [MonitoringIntervalHours] = CASE [Code]
                WHEN 'STARTER' THEN 168
                WHEN 'GROWTH' THEN 24
                WHEN 'BUSINESS' THEN 6
                WHEN 'AGENCY' THEN 1
                ELSE [MonitoringIntervalHours] END;
            """, cancellationToken);

        await CreateAsync(db, "MonitoringSchedule", """
            CREATE TABLE [dp].[MonitoringSchedule] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [IntervalHours] int NOT NULL,
                [Enabled] bit NOT NULL,
                [LastRunAtUtc] datetimeoffset NULL,
                [NextRunAtUtc] datetimeoffset NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_MonitoringSchedule] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_MonitoringSchedule_BusinessId] ON [dp].[MonitoringSchedule] ([BusinessId]);
            CREATE INDEX [IX_MonitoringSchedule_NextRunAtUtc] ON [dp].[MonitoringSchedule] ([NextRunAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "MonitoringRun", """
            CREATE TABLE [dp].[MonitoringRun] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Trigger] nvarchar(16) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Summary] nvarchar(500) NOT NULL,
                [StartedAtUtc] datetimeoffset NOT NULL,
                [CompletedAtUtc] datetimeoffset NULL,
                CONSTRAINT [PK_MonitoringRun] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_MonitoringRun_BusinessId_StartedAtUtc] ON [dp].[MonitoringRun] ([BusinessId], [StartedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "MonitoringResult", """
            CREATE TABLE [dp].[MonitoringResult] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [RunId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(40) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Title] nvarchar(160) NOT NULL,
                [ObservedFact] nvarchar(500) NOT NULL,
                [Recommendation] nvarchar(500) NOT NULL,
                [PreviousValue] nvarchar(160) NULL,
                [CurrentValue] nvarchar(160) NULL,
                CONSTRAINT [PK_MonitoringResult] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_MonitoringResult_RunId_Kind] ON [dp].[MonitoringResult] ([RunId], [Kind]);
            """, cancellationToken);

        await CreateAsync(db, "MonitoringAlert", """
            CREATE TABLE [dp].[MonitoringAlert] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ResultId] uniqueidentifier NULL,
                [Severity] nvarchar(16) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Title] nvarchar(160) NOT NULL,
                [Detail] nvarchar(500) NOT NULL,
                [OpenedAtUtc] datetimeoffset NOT NULL,
                [AcknowledgedAtUtc] datetimeoffset NULL,
                CONSTRAINT [PK_MonitoringAlert] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_MonitoringAlert_BusinessId_Status_OpenedAtUtc] ON [dp].[MonitoringAlert] ([BusinessId], [Status], [OpenedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "Competitor", """
            CREATE TABLE [dp].[Competitor] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Name] nvarchar(160) NOT NULL,
                [Website] nvarchar(2048) NULL,
                [Notes] nvarchar(500) NULL,
                CONSTRAINT [PK_Competitor] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Competitor_BusinessId_Name] ON [dp].[Competitor] ([BusinessId], [Name]);
            """, cancellationToken);

        await CreateAsync(db, "CompetitorObservation", """
            CREATE TABLE [dp].[CompetitorObservation] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [CompetitorId] uniqueidentifier NOT NULL,
                [RunId] uniqueidentifier NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Detail] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_CompetitorObservation] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_CompetitorObservation_CompetitorId_RunId] ON [dp].[CompetitorObservation] ([CompetitorId], [RunId]);
            """, cancellationToken);

        await CreateAsync(db, "PresenceReport", """
            CREATE TABLE [dp].[PresenceReport] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(16) NOT NULL,
                [Title] nvarchar(160) NOT NULL,
                [ObservedFact] nvarchar(4000) NOT NULL,
                [Recommendation] nvarchar(4000) NOT NULL,
                [AiInterpretation] nvarchar(4000) NOT NULL,
                [CustomerDecision] nvarchar(500) NULL,
                [PeriodStartUtc] datetimeoffset NOT NULL,
                [PeriodEndUtc] datetimeoffset NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_PresenceReport] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_PresenceReport_BusinessId_CreatedAtUtc] ON [dp].[PresenceReport] ([BusinessId], [CreatedAtUtc]);
            """, cancellationToken);
    }

    private static Task CreateAsync(AppDbContext db, string table, string sql, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            string.Concat("IF OBJECT_ID(N'dp.", table, "', N'U') IS NULL BEGIN ", sql, " END"),
            cancellationToken);
}
