namespace XtremeIdiots.Portal.Repository.Abstractions.Models
{
    public class ApiInfoDto
    {
        public string Version { get; set; } = string.Empty;
        public string BuildVersion { get; set; } = string.Empty;
        public string AssemblyVersion { get; set; } = string.Empty;
    }
}
