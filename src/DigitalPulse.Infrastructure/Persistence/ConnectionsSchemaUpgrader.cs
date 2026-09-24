using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class ConnectionsSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.SubscriptionPlan', 'MaxConnections') IS NULL
                ALTER TABLE [dp].[SubscriptionPlan] ADD [MaxConnections] int NOT NULL CONSTRAINT [DF_SubscriptionPlan_MaxConnections] DEFAULT 5;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [dp].[SubscriptionPlan] SET [MaxConnections] = CASE [Code]
                WHEN 'STARTER' THEN 5
                WHEN 'GROWTH' THEN 15
                WHEN 'BUSINESS' THEN 50
                WHEN 'AGENCY' THEN 500
                ELSE [MaxConnections] END;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dp.PlatformConnection', N'U') IS NULL
            BEGIN
                CREATE TABLE [dp].[PlatformConnection] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [PlatformCode] nvarchar(32) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [AuthMode] nvarchar(32) NOT NULL,
                    [ExternalAccount] nvarchar(160) NULL,
                    [GrantKind] nvarchar(32) NULL,
                    [GrantReference] nvarchar(64) NULL,
                    [AuthorizationState] nvarchar(64) NULL,
                    [ConnectedAtUtc] datetimeoffset NULL,
                    [LastHealthAtUtc] datetimeoffset NULL,
                    [LastHealthStatus] nvarchar(32) NULL,
                    [LastError] nvarchar(500) NULL,
                    CONSTRAINT [PK_PlatformConnection] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_PlatformConnection_BusinessId_PlatformCode] ON [dp].[PlatformConnection] ([BusinessId], [PlatformCode]);
                CREATE INDEX [IX_PlatformConnection_AuthorizationState] ON [dp].[PlatformConnection] ([AuthorizationState]);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('dp.PlatformConnection', 'AccessToken') IS NULL
                ALTER TABLE [dp].[PlatformConnection] ADD [AccessToken] nvarchar(4000) NULL;
            IF COL_LENGTH('dp.PlatformConnection', 'RefreshToken') IS NULL
                ALTER TABLE [dp].[PlatformConnection] ADD [RefreshToken] nvarchar(4000) NULL;
            IF COL_LENGTH('dp.PlatformConnection', 'TokenExpiresAtUtc') IS NULL
                ALTER TABLE [dp].[PlatformConnection] ADD [TokenExpiresAtUtc] datetimeoffset NULL;
            IF COL_LENGTH('dp.PlatformConnection', 'TokenScope') IS NULL
                ALTER TABLE [dp].[PlatformConnection] ADD [TokenScope] nvarchar(500) NULL;
            """, cancellationToken);
    }
}
