using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class DemosMappingExtensions
    {
        public static DemoDto ToDto(this Demo entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new DemoDto
            {
                DemoId = entity.DemoId,
                UserProfileId = entity.UserProfileId ?? Guid.Empty,
                GameType = entity.GameType.ToGameType(),
                Title = entity.Title ?? string.Empty,
                FileName = entity.FileName ?? string.Empty,
                Created = entity.Created,
                Map = entity.Map ?? string.Empty,
                Mod = entity.Mod ?? string.Empty,
                GameMode = entity.GameMode ?? string.Empty,
                ServerName = entity.ServerName ?? string.Empty,
                FileSize = entity.FileSize,
                FileUri = entity.FileUri ?? string.Empty,
                UserProfile = expand && entity.UserProfile != null ? entity.UserProfile.ToDto(false) : null
            };
        }
    }
}