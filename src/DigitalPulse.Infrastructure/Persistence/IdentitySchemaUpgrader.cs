using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class IdentitySchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        string[] statements =
        [
            """
            IF COL_LENGTH('dp.Business', 'FoundedYear') IS NULL
                ALTER TABLE [dp].[Business] ADD [FoundedYear] int NULL;
            """,
            """
            IF COL_LENGTH('dp.Business', 'BrandVoice') IS NULL
                ALTER TABLE [dp].[Business] ADD [BrandVoice] nvarchar(2000) NULL;
            """,
            """
            IF COL_LENGTH('dp.Business', 'IndustryCode') IS NULL
                ALTER TABLE [dp].[Business] ADD [IndustryCode] nvarchar(32) NULL;
            """,
            CreateTable("Industry", """
                CREATE TABLE [dp].[Industry] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [Code] nvarchar(32) NOT NULL,
                    [Name] nvarchar(120) NOT NULL,
                    CONSTRAINT [PK_Industry] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Industry_Code] ON [dp].[Industry] ([Code]);
                """),
            CreateTable("FactType", """
                CREATE TABLE [dp].[FactType] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [Code] nvarchar(32) NOT NULL,
                    [Name] nvarchar(120) NOT NULL,
                    CONSTRAINT [PK_FactType] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_FactType_Code] ON [dp].[FactType] ([Code]);
                """),
            CreateTable("ContactPoint", """
                CREATE TABLE [dp].[ContactPoint] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Value] nvarchar(2048) NOT NULL,
                    [Label] nvarchar(80) NULL,
                    CONSTRAINT [PK_ContactPoint] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ContactPoint_BusinessId_Kind_Value] ON [dp].[ContactPoint] ([BusinessId], [Kind], [Value]);
                """),
            CreateTable("BusinessCategory", """
                CREATE TABLE [dp].[BusinessCategory] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Name] nvarchar(120) NOT NULL,
                    CONSTRAINT [PK_BusinessCategory] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_BusinessCategory_BusinessId_Name] ON [dp].[BusinessCategory] ([BusinessId], [Name]);
                """),
            CreateTable("Service", """
                CREATE TABLE [dp].[Service] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    [Description] nvarchar(1000) NULL,
                    CONSTRAINT [PK_Service] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Service_BusinessId_Name] ON [dp].[Service] ([BusinessId], [Name]);
                """),
            CreateTable("Brand", """
                CREATE TABLE [dp].[Brand] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    CONSTRAINT [PK_Brand] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Brand_TenantId_Name] ON [dp].[Brand] ([TenantId], [Name]);
                """),
            CreateTable("BusinessBrand", """
                CREATE TABLE [dp].[BusinessBrand] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [BrandId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_BusinessBrand] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_BusinessBrand_BusinessId_BrandId] ON [dp].[BusinessBrand] ([BusinessId], [BrandId]);
                """),
            CreateTable("BusinessFact", """
                CREATE TABLE [dp].[BusinessFact] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [FactTypeCode] nvarchar(32) NOT NULL,
                    [Value] nvarchar(2000) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK_BusinessFact] PRIMARY KEY ([Id])
                );
                """),
            CreateTable("Customer", """
                CREATE TABLE [dp].[Customer] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [DisplayName] nvarchar(160) NOT NULL,
                    [Notes] nvarchar(1000) NULL,
                    CONSTRAINT [PK_Customer] PRIMARY KEY ([Id])
                );
                """),
            CreateTable("CustomerContact", """
                CREATE TABLE [dp].[CustomerContact] (
                    [Id] uniqueidentifier NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    [BusinessId] uniqueidentifier NOT NULL,
                    [CustomerId] uniqueidentifier NOT NULL,
                    [Kind] nvarchar(32) NOT NULL,
                    [Value] nvarchar(320) NOT NULL,
                    CONSTRAINT [PK_CustomerContact] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_CustomerContact_CustomerId_Kind_Value] ON [dp].[CustomerContact] ([CustomerId], [Kind], [Value]);
                """)
        ];

        foreach (var sql in statements)
        {
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private static string CreateTable(string name, string body) =>
        $"""
        IF OBJECT_ID(N'dp.{name}', N'U') IS NULL
        BEGIN
        {body}
        END
        """;
}
