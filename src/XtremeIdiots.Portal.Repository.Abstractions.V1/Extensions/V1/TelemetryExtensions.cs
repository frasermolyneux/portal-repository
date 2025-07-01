namespace XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1
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
