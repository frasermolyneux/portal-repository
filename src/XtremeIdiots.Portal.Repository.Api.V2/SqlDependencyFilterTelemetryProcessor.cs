using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace XtremeIdiots.Portal.Repository.Api.V2
{
    public sealed class SqlDependencyFilterTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor nextProcessor;

        public SqlDependencyFilterTelemetryProcessor(ITelemetryProcessor nextProcessor)
        {
            this.nextProcessor = nextProcessor ?? throw new ArgumentNullException(nameof(nextProcessor));
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
}
