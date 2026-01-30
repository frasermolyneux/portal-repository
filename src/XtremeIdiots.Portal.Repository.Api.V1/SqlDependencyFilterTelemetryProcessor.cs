using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace XtremeIdiots.Portal.Repository.Api.V1;

public sealed class SqlDependencyFilterTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor nextProcessor;

    public SqlDependencyFilterTelemetryProcessor(ITelemetryProcessor nextProcessor)
    {
        ArgumentNullException.ThrowIfNull(nextProcessor);
        this.nextProcessor = nextProcessor;
    }

    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dependency &&
            string.Equals(dependency.Type, "SQL", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        nextProcessor.Process(item);
    }
}
