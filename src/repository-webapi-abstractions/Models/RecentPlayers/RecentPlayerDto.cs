﻿using Newtonsoft.Json;

using System.Text.Json.Serialization;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.RecentPlayers
{
    public class RecentPlayerDto
    {
        [JsonProperty]
        public Guid RecentPlayerId { get; internal set; }

        [JsonProperty]
        public string? Name { get; internal set; }

        [JsonProperty]
        public string? IpAddress { get; internal set; }

        [JsonProperty]
        public double? Lat { get; internal set; }

        [JsonProperty]
        public double? Long { get; internal set; }

        [JsonProperty]
        public string? CountryCode { get; internal set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public Guid? PlayerId { get; internal set; }

        [JsonProperty]
        public Guid? GameServerId { get; internal set; }

        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        [JsonProperty]
        public PlayerDto? Player { get; internal set; }
    }
}
