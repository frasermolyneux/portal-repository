using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class ConfigurationMappingExtensions
    {
        public static ConfigurationDto ToDto(this GlobalConfiguration entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new ConfigurationDto
            {
                Namespace = entity.Namespace,
                Configuration = entity.Configuration,
                LastModifiedUtc = entity.LastModifiedUtc
            };
        }

        public static ConfigurationDto ToDto(this GameServerConfiguration entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new ConfigurationDto
            {
                Namespace = entity.Namespace,
                Configuration = entity.Configuration,
                LastModifiedUtc = entity.LastModifiedUtc
            };
        }
    }
}
