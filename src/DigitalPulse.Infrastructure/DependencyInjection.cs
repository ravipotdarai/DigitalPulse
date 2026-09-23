using DigitalPulse.Application.Abstractions;
using DigitalPulse.Infrastructure.Auth;
using DigitalPulse.Infrastructure.Billing;
using DigitalPulse.Infrastructure.Persistence;
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
