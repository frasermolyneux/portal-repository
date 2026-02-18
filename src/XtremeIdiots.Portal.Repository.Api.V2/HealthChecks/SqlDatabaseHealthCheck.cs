using Microsoft.Extensions.Diagnostics.HealthChecks;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V2.HealthChecks;

public class SqlDatabaseHealthCheck : IHealthCheck
{
    private readonly PortalDbContext _dbContext;

    public SqlDatabaseHealthCheck(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("SQL database connection is healthy")
                : HealthCheckResult.Unhealthy("SQL database connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL database connection failed", ex);
        }
    }
}
