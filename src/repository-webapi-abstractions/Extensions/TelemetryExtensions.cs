namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions
{
    public static class TelemetryExtensions
    {
        public static void AddAdditionalProperties(this Dictionary<string, string> telemetryProperties, Dictionary<string, string> additionalProperties)
        {
            foreach (var property in additionalProperties)
            {
                if (!telemetryProperties.ContainsKey(property.Key))
                {
                    telemetryProperties.Add(property.Key, property.Value);
                }
            }
        }
    }
}
