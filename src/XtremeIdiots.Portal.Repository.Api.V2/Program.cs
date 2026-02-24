using Azure.Identity;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Web;

using Newtonsoft.Json.Converters;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Api.V2;
using Asp.Versioning;
using XtremeIdiots.Portal.Repository.Api.V2.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
var isAzureAppConfigurationEnabled = false;

if (!string.IsNullOrWhiteSpace(appConfigEndpoint))
{
    var managedIdentityClientId = builder.Configuration["AzureAppConfiguration:ManagedIdentityClientId"];
    var environmentLabel = builder.Configuration["AzureAppConfiguration:Environment"];

    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = managedIdentityClientId,
    });

    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), credential)
            .Select("XtremeIdiots.Portal.Repository.Api.V2:*", environmentLabel)
            .TrimKeyPrefix("XtremeIdiots.Portal.Repository.Api.V2:")
            .Select("SqlResilience:*", environmentLabel)
            .ConfigureRefresh(refresh =>
            {
                refresh.Register("Sentinel", environmentLabel, refreshAll: true)
                       .SetRefreshInterval(TimeSpan.FromMinutes(5));
            });

        options.ConfigureKeyVault(kv =>
        {
            kv.SetCredential(credential);
            kv.SetSecretRefreshInterval(TimeSpan.FromHours(1));
        });
    });

    builder.Services.AddAzureAppConfiguration();
    isAzureAppConfigurationEnabled = true;
}

builder.Services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
builder.Services.AddLogging();
builder.Services.AddMemoryCache();

//https://learn.microsoft.com/en-us/azure/azure-monitor/app/sampling-classic-api#configure-sampling-settings
var samplingSettings = new SamplingPercentageEstimatorSettings
{
    InitialSamplingPercentage = double.TryParse(builder.Configuration["ApplicationInsights:InitialSamplingPercentage"], out var initPct) ? initPct : 5,
    MinSamplingPercentage = double.TryParse(builder.Configuration["ApplicationInsights:MinSamplingPercentage"], out var minPct) ? minPct : 5,
    MaxSamplingPercentage = double.TryParse(builder.Configuration["ApplicationInsights:MaxSamplingPercentage"], out var maxPct) ? maxPct : 60
};

builder.Services.Configure<TelemetryConfiguration>(telemetryConfiguration =>
{
    var telemetryProcessorChainBuilder = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    telemetryProcessorChainBuilder.Use(next => new SqlDependencyFilterTelemetryProcessor(next));
    telemetryProcessorChainBuilder.UseAdaptiveSampling(
        settings: samplingSettings,
        callback: null,
        excludedTypes: "Exception");
    telemetryProcessorChainBuilder.Build();
});

builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
{
    EnableAdaptiveSampling = false,
});

builder.Services.AddServiceProfiler();

builder.Services.AddDbContext<PortalDbContext>(options =>
{
    var retryCount = int.TryParse(builder.Configuration["SqlResilience:RetryCount"], out var rc) ? rc : 3;
    var retryDelay = int.TryParse(builder.Configuration["SqlResilience:RetryDelaySeconds"], out var rd) ? rd : 5;
    var commandTimeout = int.TryParse(builder.Configuration["SqlResilience:CommandTimeoutSeconds"], out var ct) ? ct : 180;

    options.UseSqlServer(builder.Configuration["sql_connection_string"], sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(retryCount, TimeSpan.FromSeconds(retryDelay), null);
        sqlOptions.CommandTimeout(commandTimeout);
    });
});

// Add services to the container.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.Converters.Add(new StringEnumConverter());
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

// Configure API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    // Configure URL path versioning
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    // Format the version as "'v'major.minor" (e.g. v2.0)
    options.GroupNameFormat = "'v'VV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure OpenAPI
builder.Services.AddOpenApi("v2.0", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer<StripVersionPrefixTransformer>();
});

builder.Services.AddHealthChecks()
    .AddCheck<XtremeIdiots.Portal.Repository.Api.V2.HealthChecks.SqlDatabaseHealthCheck>(
        name: "sql-database",
        tags: ["dependency"]);

var app = builder.Build();

if (isAzureAppConfigurationEnabled)
{
    app.UseAzureAppConfiguration();
}

// Update adaptive sampling settings when configuration refreshes
ChangeToken.OnChange(
    () => app.Configuration.GetReloadToken(),
    () =>
    {
        if (double.TryParse(app.Configuration["ApplicationInsights:MinSamplingPercentage"], out var min))
            samplingSettings.MinSamplingPercentage = min;
        if (double.TryParse(app.Configuration["ApplicationInsights:MaxSamplingPercentage"], out var max))
            samplingSettings.MaxSamplingPercentage = max;
    });

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok()).ExcludeFromDescription();

app.Run();

public partial class Program { }

