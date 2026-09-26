using DigitalPulse.Application.Abstractions;
using DigitalPulse.Infrastructure.Actions;
using DigitalPulse.Infrastructure.Ai;
using DigitalPulse.Infrastructure.Auth;
using DigitalPulse.Infrastructure.Billing;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Platforms;
using DigitalPulse.Infrastructure.Reports;
using DigitalPulse.Infrastructure.WhatsApp;
using DigitalPulse.Infrastructure.Monitoring;
using DigitalPulse.Infrastructure.Operations;
using DigitalPulse.Infrastructure.Scanning;
using DigitalPulse.Infrastructure.Search;
using DigitalPulse.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? "Server=localhost,1433;Database=DigitalPulse;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30";

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<DevelopmentJwtTokenIssuer>();
        services.AddSingleton<IAuthTokenIssuer>(sp => sp.GetRequiredService<DevelopmentJwtTokenIssuer>());
        services.AddSingleton<IPasswordService, AspNetPasswordService>();
        services.AddScoped<ISubscriptionPlanCatalog, EfSubscriptionPlanCatalog>();
        if (!string.IsNullOrWhiteSpace(configuration["Billing:Razorpay:KeyId"]) &&
            !string.IsNullOrWhiteSpace(configuration["Billing:Razorpay:KeySecret"]))
        {
            services.AddSingleton<IBillingGateway, RazorpayBillingGateway>();
        }
        else
        {
            services.AddSingleton<IBillingGateway, DevelopmentBillingGateway>();
        }
        services.AddHttpClient("official-platforms", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(25);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalPulse-Platforms/1.0");
        });
        services.AddSingleton<IOfficialPlatformGateway, OfficialPlatformGateway>();
        services.AddSingleton<ISocialMediaStore, FileSocialMediaStore>();
        services.AddSingleton<IPlatformAdapter, GoogleAdapter>();
        services.AddSingleton<IPlatformAdapter, FacebookAdapter>();
        services.AddSingleton<IPlatformAdapter, InstagramAdapter>();
        services.AddSingleton<IPlatformAdapter, LinkedInAdapter>();
        services.AddSingleton<IPlatformAdapter, YouTubeAdapter>();
        services.AddSingleton<IPlatformAdapter, IndiaMartAdapter>();
        services.AddSingleton<IPlatformAdapter, JustdialAdapter>();
        services.AddSingleton<IPlatformAdapter, WhatsAppAdapter>();
        services.AddSingleton<IPlatformAdapter, WebsiteAdapter>();
        services.AddSingleton<IPlatformAdapter, SearchConsoleAdapter>();
        services.AddSingleton<IPlatformAdapter, GoogleAdsAdapter>();
        services.AddSingleton<IPlatformAdapter, GoogleAnalyticsAdapter>();
        services.AddSingleton<ITestReportPdf, TestReportPdf>();
        services.AddSingleton<IPlatformAdapterCatalog, PlatformAdapterCatalog>();
        services.AddSingleton<OfficialOAuthBroker>();
        services.AddSingleton<IPlatformAuthorizationBroker>(sp => sp.GetRequiredService<OfficialOAuthBroker>());
        services.AddSingleton<ILiveTokenRefresher>(sp => sp.GetRequiredService<OfficialOAuthBroker>());
        services.AddSingleton<ISearchProvider, InMemorySearchProvider>();
        services.AddSingleton<IVectorSearchProvider, LocalHashVectorSearchProvider>();
        var timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Ai:TimeoutSeconds", 60), 5, 180));
        var selected = (configuration["Ai:Provider"] ?? string.Empty).Trim();
        var openAiKey = configuration["Ai:OpenAi:ApiKey"];
        var azureKey = configuration["Ai:AzureOpenAi:ApiKey"];
        var azureEndpoint = configuration["Ai:AzureOpenAi:Endpoint"];
        var useAzure = selected.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(azureKey)
            && !string.IsNullOrWhiteSpace(azureEndpoint);
        var useOpenAi = !useAzure
            && (string.IsNullOrWhiteSpace(selected) || selected.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(openAiKey);

        if (useAzure)
        {
            services.AddHttpClient<AzureOpenAiProvider>(client =>
            {
                client.Timeout = timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalPulse-Ai/1.0");
            });
            services.AddTransient<IAiProvider>(sp =>
                new RetryingAiProvider(sp.GetRequiredService<AzureOpenAiProvider>(), configuration));
        }
        else if (useOpenAi)
        {
            services.AddHttpClient<OpenAiProvider>(client =>
            {
                client.Timeout = timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalPulse-Ai/1.0");
            });
            services.AddTransient<IAiProvider>(sp =>
                new RetryingAiProvider(sp.GetRequiredService<OpenAiProvider>(), configuration));
        }
        else
        {
            services.AddSingleton<IAiProvider, DevelopmentAiProvider>();
        }
        var whatsAppToken = configuration["WhatsApp:CloudApi:AccessToken"];
        if (!string.IsNullOrWhiteSpace(whatsAppToken))
        {
            services.AddHttpClient<IWhatsAppCloudApi, LiveWhatsAppCloudApi>(client =>
            {
                client.BaseAddress = new Uri(configuration["WhatsApp:CloudApi:BaseUrl"] ?? "https://graph.facebook.com/v21.0/");
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalPulse-WhatsApp/1.0");
            });
        }
        else
        {
            services.AddSingleton<IWhatsAppCloudApi, DevelopmentWhatsAppCloudApi>();
        }
        if (configuration.GetValue<bool>("Testing:UseInMemory"))
        {
            services.AddSingleton<IWebsiteProbe, StubWebsiteProbe>();
            services.AddSingleton<IWebsiteFetcher, StubWebsiteFetcher>();
        }
        else
        {
            services.AddHttpClient<IWebsiteProbe, WebsiteProbe>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalPulse-Check/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectTimeout = TimeSpan.FromSeconds(5)
            });
            services.AddHttpClient<IWebsiteFetcher, WebsiteFetcher>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalPulse-Website/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectTimeout = TimeSpan.FromSeconds(5)
            });
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            if (configuration.GetValue<bool>("Testing:UseInMemory"))
            {
                options.UseInMemoryDatabase(configuration["Testing:Database"] ?? "digitalpulse-tests");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IOperationsEnvironment, ConfigurationOperationsEnvironment>();
        services.AddSingleton<IPackageInventory, FilePackageInventory>();
        if (!configuration.GetValue<bool>("Testing:UseInMemory"))
        {
            services.AddHostedService<MonitoringTicker>();
            services.AddHostedService<ActionDispatchTicker>();
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var issuer = new DevelopmentJwtTokenIssuer(configuration);
                options.TokenValidationParameters = issuer.TokenValidationParameters();
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Invalid leftover tokens must not fail anonymous routes such as /v1/auth/login.
                        context.NoResult();
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();
        return services;
    }
}
