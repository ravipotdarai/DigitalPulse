using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class WhatsAppSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.SubscriptionPlan', 'WhatsAppEnabled') IS NULL
                ALTER TABLE [dp].[SubscriptionPlan] ADD [WhatsAppEnabled] bit NOT NULL CONSTRAINT [DF_SubscriptionPlan_WhatsAppEnabled] DEFAULT 0;
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.SubscriptionPlan', 'WhatsAppMessagesPerMonth') IS NULL
                ALTER TABLE [dp].[SubscriptionPlan] ADD [WhatsAppMessagesPerMonth] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_WhatsAppMessagesPerMonth] DEFAULT 0;
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [dp].[SubscriptionPlan] SET
                [WhatsAppEnabled] = CASE [Code] WHEN 'STARTER' THEN 0 ELSE 1 END,
                [WhatsAppMessagesPerMonth] = CASE [Code]
                    WHEN 'STARTER' THEN 0
                    WHEN 'GROWTH' THEN 2000
                    WHEN 'BUSINESS' THEN 10000
                    WHEN 'AGENCY' THEN 50000
                    ELSE [WhatsAppMessagesPerMonth] END;
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppAccount", """
            CREATE TABLE [dp].[WhatsAppAccount] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ConnectionId] uniqueidentifier NULL,
                [DisplayName] nvarchar(160) NOT NULL,
                [WabaId] nvarchar(64) NULL,
                [PhoneNumber] nvarchar(32) NOT NULL,
                [Status] nvarchar(32) NOT NULL,
                [PhoneStatus] nvarchar(32) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                [PhoneVerifiedAtUtc] datetimeoffset NULL,
                [LastHealthAtUtc] datetimeoffset NULL,
                [LastHealthDetail] nvarchar(500) NULL,
                CONSTRAINT [PK_WhatsAppAccount] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_WhatsAppAccount_BusinessId] ON [dp].[WhatsAppAccount] ([BusinessId]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppContact", """
            CREATE TABLE [dp].[WhatsAppContact] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [CustomerId] uniqueidentifier NULL,
                [CustomerContactId] uniqueidentifier NULL,
                [DisplayName] nvarchar(160) NOT NULL,
                [Mobile] nvarchar(32) NOT NULL,
                [Consent] nvarchar(16) NOT NULL,
                [OptedInAtUtc] datetimeoffset NULL,
                [OptedOutAtUtc] datetimeoffset NULL,
                [LastInboundAtUtc] datetimeoffset NULL,
                CONSTRAINT [PK_WhatsAppContact] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_WhatsAppContact_BusinessId_Mobile] ON [dp].[WhatsAppContact] ([BusinessId], [Mobile]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppTemplate", """
            CREATE TABLE [dp].[WhatsAppTemplate] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Name] nvarchar(80) NOT NULL,
                [Language] nvarchar(16) NOT NULL,
                [Category] nvarchar(32) NOT NULL,
                [Body] nvarchar(1024) NOT NULL,
                [Status] nvarchar(32) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_WhatsAppTemplate] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_WhatsAppTemplate_BusinessId_Name] ON [dp].[WhatsAppTemplate] ([BusinessId], [Name]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppCampaign", """
            CREATE TABLE [dp].[WhatsAppCampaign] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [TemplateId] uniqueidentifier NOT NULL,
                [Name] nvarchar(160) NOT NULL,
                [Status] nvarchar(32) NOT NULL,
                [ScheduledAtUtc] datetimeoffset NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                [AudienceCount] int NOT NULL,
                [SendCount] int NOT NULL,
                [HeldCount] int NOT NULL,
                [FailedCount] int NOT NULL,
                CONSTRAINT [PK_WhatsAppCampaign] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_WhatsAppCampaign_BusinessId_UpdatedAtUtc] ON [dp].[WhatsAppCampaign] ([BusinessId], [UpdatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppConversation", """
            CREATE TABLE [dp].[WhatsAppConversation] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ContactId] uniqueidentifier NOT NULL,
                [LastInboundAtUtc] datetimeoffset NULL,
                [LastOutboundAtUtc] datetimeoffset NULL,
                [WindowOpenUntilUtc] datetimeoffset NULL,
                CONSTRAINT [PK_WhatsAppConversation] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_WhatsAppConversation_BusinessId_ContactId] ON [dp].[WhatsAppConversation] ([BusinessId], [ContactId]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppMessage", """
            CREATE TABLE [dp].[WhatsAppMessage] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [ContactId] uniqueidentifier NOT NULL,
                [ConversationId] uniqueidentifier NULL,
                [CampaignId] uniqueidentifier NULL,
                [TemplateId] uniqueidentifier NULL,
                [Kind] nvarchar(16) NOT NULL,
                [Status] nvarchar(32) NOT NULL,
                [Body] nvarchar(4000) NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                [ProviderMessageId] nvarchar(128) NULL,
                [Untrusted] bit NOT NULL,
                CONSTRAINT [PK_WhatsAppMessage] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_WhatsAppMessage_BusinessId_UpdatedAtUtc] ON [dp].[WhatsAppMessage] ([BusinessId], [UpdatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppMessageAttempt", """
            CREATE TABLE [dp].[WhatsAppMessageAttempt] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [MessageId] uniqueidentifier NOT NULL,
                [Ordinal] int NOT NULL,
                [Outcome] nvarchar(32) NOT NULL,
                [Detail] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_WhatsAppMessageAttempt] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_WhatsAppMessageAttempt_MessageId_Ordinal] ON [dp].[WhatsAppMessageAttempt] ([MessageId], [Ordinal]);
            """, cancellationToken);

        await CreateAsync(db, "WhatsAppWebhookEvent", """
            CREATE TABLE [dp].[WhatsAppWebhookEvent] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [BusinessId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(40) NOT NULL,
                [Detail] nvarchar(500) NOT NULL,
                [Trusted] bit NOT NULL,
                CONSTRAINT [PK_WhatsAppWebhookEvent] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_WhatsAppWebhookEvent_BusinessId_CreatedAtUtc] ON [dp].[WhatsAppWebhookEvent] ([BusinessId], [CreatedAtUtc]);
            """, cancellationToken);
    }

    private static Task CreateAsync(AppDbContext db, string table, string sql, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            string.Concat("IF OBJECT_ID(N'dp.", table, "', N'U') IS NULL BEGIN ", sql, " END"),
            cancellationToken);
}
