using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class DemosMappingExtensions
    {
        public static DemoDto ToDto(this Demo entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new DemoDto
            {
                DemoId = entity.DemoId,
                GameServerId = entity.GameServerId,
                FileName = entity.FileName ?? string.Empty,
                Size = entity.Size,
                Created = entity.Created,
                GameType = entity.GameType.ToGameType()
            };
        }
    }
}