using Azure.Identity;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

using Newtonsoft.Json.Converters;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Api.V2;
using Asp.Versioning;
using XtremeIdiots.Portal.Repository.Api.V2.OpenApi;
using Azure.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var appConfigurationEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
var isAzureAppConfigurationEnabled = false;

if (!string.IsNullOrWhiteSpace(appConfigurationEndpoint))
{
    var managedIdentityClientId = builder.Configuration["AzureAppConfiguration:ManagedIdentityClientId"];
    TokenCredential identityCredential = string.IsNullOrWhiteSpace(managedIdentityClientId)
        ? new DefaultAzureCredential()
        : new ManagedIdentityCredential(managedIdentityClientId);

    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigurationEndpoint), identityCredential)
               .Select("XtremeIdiots.Portal.Repository.Api.V2:*", labelFilter: builder.Configuration["AzureAppConfiguration:Environment"])
               .TrimKeyPrefix("XtremeIdiots.Portal.Repository.Api.V2:");

        options.ConfigureKeyVault(keyVaultOptions =>
        {
            keyVaultOptions.SetCredential(identityCredential);
        });
    });

    builder.Services.AddAzureAppConfiguration();
    isAzureAppConfigurationEnabled = true;
}

builder.Services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
builder.Services.AddLogging();
builder.Services.AddMemoryCache();

//https://learn.microsoft.com/en-us/azure/azure-monitor/app/sampling-classic-api#configure-sampling-settings
builder.Services.Configure<TelemetryConfiguration>(telemetryConfiguration =>
{
    var telemetryProcessorChainBuilder = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    telemetryProcessorChainBuilder.Use(next => new SqlDependencyFilterTelemetryProcessor(next));
    telemetryProcessorChainBuilder.UseAdaptiveSampling(
        settings: new SamplingPercentageEstimatorSettings
        {
            InitialSamplingPercentage = 5,
            MinSamplingPercentage = 5,
            MaxSamplingPercentage = 60
        },
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
    options.UseSqlServer(builder.Configuration["sql_connection_string"], sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        sqlOptions.CommandTimeout(180);
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

builder.Services.AddHealthChecks();

var app = builder.Build();

if (isAzureAppConfigurationEnabled)
{
    app.UseAzureAppConfiguration();
}

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

