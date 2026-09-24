using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class BillingSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await AddColumnAsync(db, "SubscriptionPlan", "AnnualPriceInr", "[AnnualPriceInr] decimal(19,4) NOT NULL CONSTRAINT [DF_SubscriptionPlan_AnnualPriceInr] DEFAULT 0", cancellationToken);
        await AddColumnAsync(db, "SubscriptionPlan", "MaxLocations", "[MaxLocations] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_MaxLocations] DEFAULT 1", cancellationToken);
        await AddColumnAsync(db, "SubscriptionPlan", "AiGenerationsPerMonth", "[AiGenerationsPerMonth] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_AiGenerationsPerMonth] DEFAULT 50", cancellationToken);
        await AddColumnAsync(db, "SubscriptionPlan", "MaxUsers", "[MaxUsers] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_MaxUsers] DEFAULT 2", cancellationToken);
        await AddColumnAsync(db, "SubscriptionPlan", "MaxAgencyClients", "[MaxAgencyClients] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_MaxAgencyClients] DEFAULT 0", cancellationToken);
        await AddColumnAsync(db, "SubscriptionPlan", "StorageGb", "[StorageGb] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_StorageGb] DEFAULT 2", cancellationToken);
        await AddColumnAsync(db, "SubscriptionPlan", "WhiteLabel", "[WhiteLabel] bit NOT NULL CONSTRAINT [DF_SubscriptionPlan_WhiteLabel] DEFAULT 0", cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [dp].[SubscriptionPlan] SET
                [AnnualPriceInr] = CASE [Code] WHEN 'STARTER' THEN 29990 WHEN 'GROWTH' THEN 69990 WHEN 'BUSINESS' THEN 149990 WHEN 'AGENCY' THEN 299990 ELSE [AnnualPriceInr] END,
                [MaxLocations] = CASE [Code] WHEN 'STARTER' THEN 1 WHEN 'GROWTH' THEN 5 WHEN 'BUSINESS' THEN 25 WHEN 'AGENCY' THEN 250 ELSE [MaxLocations] END,
                [AiGenerationsPerMonth] = CASE [Code] WHEN 'STARTER' THEN 50 WHEN 'GROWTH' THEN 250 WHEN 'BUSINESS' THEN 1000 WHEN 'AGENCY' THEN 10000 ELSE [AiGenerationsPerMonth] END,
                [MaxUsers] = CASE [Code] WHEN 'STARTER' THEN 2 WHEN 'GROWTH' THEN 5 WHEN 'BUSINESS' THEN 15 WHEN 'AGENCY' THEN 100 ELSE [MaxUsers] END,
                [MaxAgencyClients] = CASE [Code] WHEN 'AGENCY' THEN 50 ELSE 0 END,
                [StorageGb] = CASE [Code] WHEN 'STARTER' THEN 2 WHEN 'GROWTH' THEN 10 WHEN 'BUSINESS' THEN 50 WHEN 'AGENCY' THEN 500 ELSE [StorageGb] END,
                [WhiteLabel] = CASE [Code] WHEN 'AGENCY' THEN 1 ELSE 0 END;
            """, cancellationToken);

        await AddColumnAsync(db, "Subscription", "Interval", "[Interval] nvarchar(16) NOT NULL CONSTRAINT [DF_Subscription_Interval] DEFAULT 'Monthly'", cancellationToken);
        await AddColumnAsync(db, "Subscription", "PeriodStartUtc", "[PeriodStartUtc] datetimeoffset NOT NULL CONSTRAINT [DF_Subscription_PeriodStartUtc] DEFAULT SYSDATETIMEOFFSET()", cancellationToken);
        await AddColumnAsync(db, "Subscription", "PeriodEndUtc", "[PeriodEndUtc] datetimeoffset NOT NULL CONSTRAINT [DF_Subscription_PeriodEndUtc] DEFAULT SYSDATETIMEOFFSET()", cancellationToken);
        await AddColumnAsync(db, "Subscription", "CancelAtPeriodEnd", "[CancelAtPeriodEnd] bit NOT NULL CONSTRAINT [DF_Subscription_CancelAtPeriodEnd] DEFAULT 0", cancellationToken);
        await AddColumnAsync(db, "Subscription", "CancelledAtUtc", "[CancelledAtUtc] datetimeoffset NULL", cancellationToken);
        await AddColumnAsync(db, "Subscription", "ProviderCode", "[ProviderCode] nvarchar(40) NULL", cancellationToken);
        await AddColumnAsync(db, "Subscription", "ProviderSubscriptionId", "[ProviderSubscriptionId] nvarchar(80) NULL", cancellationToken);
        await AddColumnAsync(db, "Subscription", "HoldReason", "[HoldReason] nvarchar(500) NOT NULL CONSTRAINT [DF_Subscription_HoldReason] DEFAULT N'Plan selected. Payment stays held until a live billing provider confirms it.'", cancellationToken);

        await CreateAsync(db, "Invoice", """
            CREATE TABLE [dp].[Invoice] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [SubscriptionId] uniqueidentifier NOT NULL,
                [Number] nvarchar(40) NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Interval] nvarchar(16) NOT NULL,
                [AmountInr] decimal(19,4) NOT NULL,
                [PeriodStartUtc] datetimeoffset NOT NULL,
                [PeriodEndUtc] datetimeoffset NOT NULL,
                [IssuedAtUtc] datetimeoffset NOT NULL,
                [PaidAtUtc] datetimeoffset NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_Invoice] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Invoice_TenantId_Number] ON [dp].[Invoice] ([TenantId], [Number]);
            """, cancellationToken);

        await CreateAsync(db, "InvoiceLine", """
            CREATE TABLE [dp].[InvoiceLine] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [InvoiceId] uniqueidentifier NOT NULL,
                [Description] nvarchar(240) NOT NULL,
                [AmountInr] decimal(19,4) NOT NULL,
                CONSTRAINT [PK_InvoiceLine] PRIMARY KEY ([Id])
            );
            """, cancellationToken);

        await CreateAsync(db, "PaymentAttempt", """
            CREATE TABLE [dp].[PaymentAttempt] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [InvoiceId] uniqueidentifier NOT NULL,
                [Status] nvarchar(16) NOT NULL,
                [Provider] nvarchar(40) NOT NULL,
                [ProviderReference] nvarchar(80) NULL,
                [Detail] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_PaymentAttempt] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_PaymentAttempt_InvoiceId_CreatedAtUtc] ON [dp].[PaymentAttempt] ([InvoiceId], [CreatedAtUtc]);
            """, cancellationToken);

        await CreateAsync(db, "UsageRecord", """
            CREATE TABLE [dp].[UsageRecord] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Kind] nvarchar(24) NOT NULL,
                [Quantity] int NOT NULL,
                [PeriodStartUtc] datetimeoffset NOT NULL,
                [Source] nvarchar(160) NOT NULL,
                CONSTRAINT [PK_UsageRecord] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_UsageRecord_TenantId_Kind_PeriodStartUtc] ON [dp].[UsageRecord] ([TenantId], [Kind], [PeriodStartUtc]);
            """, cancellationToken);

        await CreateAsync(db, "BillingWebhookEvent", """
            CREATE TABLE [dp].[BillingWebhookEvent] (
                [Id] uniqueidentifier NOT NULL,
                [CreatedAtUtc] datetimeoffset NOT NULL,
                [UpdatedAtUtc] datetimeoffset NOT NULL,
                [TenantId] uniqueidentifier NULL,
                [Provider] nvarchar(40) NOT NULL,
                [EventType] nvarchar(80) NOT NULL,
                [Payload] nvarchar(4000) NOT NULL,
                [SignatureValid] bit NOT NULL,
                [Untrusted] bit NOT NULL,
                [Processed] bit NOT NULL,
                [HoldReason] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_BillingWebhookEvent] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_BillingWebhookEvent_CreatedAtUtc] ON [dp].[BillingWebhookEvent] ([CreatedAtUtc]);
            """, cancellationToken);
    }

    private static Task AddColumnAsync(AppDbContext db, string table, string column, string definition, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            string.Concat("IF COL_LENGTH('dp.", table, "', '", column, "') IS NULL ALTER TABLE [dp].[", table, "] ADD ", definition, ";"),
            cancellationToken);

    private static Task CreateAsync(AppDbContext db, string table, string sql, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            string.Concat("IF OBJECT_ID(N'dp.", table, "', N'U') IS NULL BEGIN ", sql, " END"),
            cancellationToken);
}
