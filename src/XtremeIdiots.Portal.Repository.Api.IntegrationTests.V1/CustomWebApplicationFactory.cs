using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables before the host builds so Program.cs reads them
        // This prevents Azure App Configuration from connecting
        Environment.SetEnvironmentVariable("AzureAppConfiguration__Endpoint", "");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear all config sources to prevent user secrets from providing real AzureAd credentials
            config.Sources.Clear();

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAppConfiguration:Endpoint"] = "",
                ["sql_connection_string"] = "",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientSecret"] = "",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove ALL EF Core and DbContext-related registrations aggressively
            // to avoid the dual database provider conflict (SqlServer + InMemory)
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<PortalDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(PortalDbContext) ||
                    (d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true) ||
                    (d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true) ||
                    (d.ServiceType.FullName?.Contains("SqlServer") == true) ||
                    (d.ImplementationType?.FullName?.Contains("SqlServer") == true))
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Add in-memory database
            services.AddDbContext<PortalDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Provide a stub TelemetryConfiguration
            services.RemoveAll<TelemetryConfiguration>();
            var telemetryConfig = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost"
            };
            services.AddSingleton(telemetryConfig);

            // Remove ServiceProfiler hosted services
            var profilerDescriptors = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                            d.ImplementationType?.FullName?.Contains("Profiler") == true)
                .ToList();
            foreach (var descriptor in profilerDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove real health checks that need SQL connection
            services.RemoveAll<Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck>();

            // Remove all existing authentication/authorization services and replace with test scheme
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider>();
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });

        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Seeds the in-memory database using the provided action.
    /// </summary>
    public void SeedDatabase(Action<PortalDbContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        context.Database.EnsureCreated();
        seedAction(context);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, "ServiceAccount"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
