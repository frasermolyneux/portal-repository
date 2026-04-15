using Azure.Identity;
using Azure.Data.Tables;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

using Newtonsoft.Json.Converters;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Api.V1;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using Asp.Versioning;
using XtremeIdiots.Portal.Repository.Api.V1.OpenApi;
using MX.Observability.ApplicationInsights.Extensions;
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
            .Select("XtremeIdiots.Portal.Repository.Api.V1:*", environmentLabel)
            .TrimKeyPrefix("XtremeIdiots.Portal.Repository.Api.V1:")
            .Select("GameTracker:*", environmentLabel)
            .Select("SqlResilience:*", environmentLabel)
            .Select("ApplicationInsights:*", environmentLabel)
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
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
});

builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
{
    EnableAdaptiveSampling = false,
});

builder.Services.AddObservability();
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
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    // Configure URL path versioning
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    // Format the version as "'v'major.minor" (e.g. v1.0)
    options.GroupNameFormat = "'v'VV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure OpenAPI
builder.Services.AddOpenApi("v1.0", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer<StripVersionPrefixTransformer>();
});

builder.Services.AddHealthChecks()
    .AddCheck<XtremeIdiots.Portal.Repository.Api.V1.HealthChecks.SqlDatabaseHealthCheck>(
        name: "sql-database",
        tags: ["dependency"]);

// Register Table Storage for live status
var tableEndpoint = builder.Configuration["appdata_storage_table_endpoint"];
if (!string.IsNullOrWhiteSpace(tableEndpoint))
{
    var managedIdentityClientId = builder.Configuration["AzureAppConfiguration:ManagedIdentityClientId"];
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = managedIdentityClientId,
    });
    builder.Services.AddSingleton(new TableServiceClient(new Uri(tableEndpoint), credential));
    builder.Services.AddSingleton<ILiveStatusStore, TableStorageLiveStatusStore>();
}
else
{
    // Development fallback: use Azurite or connection string
    var connectionString = builder.Configuration["appdata_storage_connection_string"];
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        builder.Services.AddSingleton(new TableServiceClient(connectionString));
        builder.Services.AddSingleton<ILiveStatusStore, TableStorageLiveStatusStore>();
    }
    else
    {
        builder.Services.AddSingleton<ILiveStatusStore, NullLiveStatusStore>();
    }
}

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

app.MapGet("/", () => Results.Ok()).ExcludeFromDescription();

app.Run();

public partial class Program { }
