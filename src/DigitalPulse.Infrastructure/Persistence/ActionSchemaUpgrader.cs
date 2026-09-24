using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class ActionSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.SubscriptionPlan', 'ActionsPerMonth') IS NULL
                ALTER TABLE [dp].[SubscriptionPlan] ADD [ActionsPerMonth] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_ActionsPerMonth] DEFAULT 25;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [dp].[SubscriptionPlan] SET [ActionsPerMonth] = CASE [Code]
                WHEN 'STARTER' THEN 25
                WHEN 'GROWTH' THEN 150
                WHEN 'BUSINESS' THEN 750
                WHEN 'AGENCY' THEN 10000
                ELSE [ActionsPerMonth] END;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.AutomationPolicy', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[AutomationPolicy] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [Mode] nvarchar(32) NOT NULL,
                    [AllowLowRiskAuto] bit NOT NULL,
                    [RequireApprovalForHighRisk] bit NOT NULL,
                    [MaxAttempts] int NOT NULL,
                    CONSTRAINT [PK_AutomationPolicy] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_AutomationPolicy_TenantId] ON [dp].[AutomationPolicy] ([TenantId]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.WorkAction', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[WorkAction] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(40) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Risk] nvarchar(16) NOT NULL,
                    [Title] nvarchar(160) NOT NULL,
                    [IdempotencyKey] nvarchar(160) NOT NULL,
                    [TargetId] uniqueidentifier NULL,
                    [TargetLabel] nvarchar(160) NULL,
                    [LiveWriteAvailable] bit NOT NULL,
                    [AutopilotEligible] bit NOT NULL,
                    [HoldReason] nvarchar(500) NOT NULL,
                    [AttemptCount] int NOT NULL,
                    [NextRetryAtUtc] datetimeoffset NULL,
                    [ApprovedAtUtc] datetimeoffset NULL,
                    [ExecutedAtUtc] datetimeoffset NULL,
                    CONSTRAINT [PK_WorkAction] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_WorkAction_BusinessId_UpdatedAtUtc] ON [dp].[WorkAction] ([BusinessId], [UpdatedAtUtc]);
                CREATE INDEX [IX_WorkAction_TenantId_IdempotencyKey_Status] ON [dp].[WorkAction] ([TenantId], [IdempotencyKey], [Status]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ActionAttempt', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ActionAttempt] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [WorkActionId] uniqueidentifier NOT NULL,
                    [Ordinal] int NOT NULL,
                    [Outcome] nvarchar(32) NOT NULL,
                    [Detail] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_ActionAttempt] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ActionAttempt_WorkActionId_Ordinal] ON [dp].[ActionAttempt] ([WorkActionId], [Ordinal]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.ActionVerification', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[ActionVerification] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [WorkActionId] uniqueidentifier NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Detail] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_ActionVerification] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_ActionVerification_WorkActionId_CreatedAtUtc] ON [dp].[ActionVerification] ([WorkActionId], [CreatedAtUtc]);
            END
            """, cancellationToken);
    }
}
