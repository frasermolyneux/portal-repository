namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models
{
    internal interface IDto
    {
        public Dictionary<string, string> TelemetryProperties { get; }
    }
}
