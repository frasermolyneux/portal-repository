using MX.GeoLocation.Abstractions.Models.V1_1;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for LivePlayer entities and DTOs.
    /// </summary>
    public static class LivePlayersMappingExtensions
    {
        /// <summary>
        /// Maps a LivePlayer entity to a LivePlayerDto.
        /// NOTE: Player navigation property will be null to avoid circular dependencies.
        /// </summary>
        /// <param name="entity">The LivePlayer entity to map from.</param>
        /// <returns>The mapped LivePlayerDto.</returns>
        public static LivePlayerDto ToDto(this LivePlayer entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            IpIntelligenceDto? geoIntelligence = null;
            if (entity.Lat.HasValue && entity.Long.HasValue)
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Latitude = entity.Lat.Value,
                    Longitude = entity.Long.Value,
                    CountryCode = entity.CountryCode
                });
                geoIntelligence = JsonConvert.DeserializeObject<IpIntelligenceDto>(json);
            }

            return new LivePlayerDto
            {
                LivePlayerId = entity.LivePlayerId,
                Name = entity.Name,
                Score = entity.Score,
                Ping = entity.Ping,
                Num = entity.Num,
                Rate = entity.Rate,
                Team = entity.Team,
                Time = entity.Time,
                IpAddress = entity.IpAddress,
                GeoIntelligence = geoIntelligence,
                GameType = entity.GameType.ToGameType(),
                PlayerId = entity.PlayerId,
                GameServerServerId = entity.GameServerId,
                Player = expand && entity.Player is not null ? entity.Player.ToDto(false) : null
            };
        }

        /// <summary>
        /// Maps a CreateLivePlayerDto to a LivePlayer entity.
        /// </summary>
        /// <param name="dto">The CreateLivePlayerDto to map from.</param>
        /// <returns>The mapped LivePlayer entity.</returns>
        public static LivePlayer ToEntity(this CreateLivePlayerDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new LivePlayer
            {
                PlayerId = dto.PlayerId,
                GameServerId = dto.GameServerId,
                Name = dto.Name ?? string.Empty,
                Score = dto.Score,
                Ping = dto.Ping,
                Num = dto.Num,
                Rate = dto.Rate,
                Team = dto.Team,
                Time = dto.Time,
                IpAddress = dto.IpAddress,
                Lat = dto.GeoIntelligence?.Latitude,
                Long = dto.GeoIntelligence?.Longitude,
                CountryCode = dto.GeoIntelligence?.CountryCode,
                GameType = dto.GameType.ToGameTypeInt()
            };
        }
    }
}