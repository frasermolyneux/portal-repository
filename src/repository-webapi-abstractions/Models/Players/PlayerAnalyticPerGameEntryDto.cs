﻿using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    public record PlayerAnalyticPerGameEntryDto : IDto
    {
        [JsonProperty]
        public DateTime Created { get; internal set; }

        [JsonProperty]
        public Dictionary<GameType, int> GameCounts { get; internal set; } = new Dictionary<GameType, int>();

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>();
                return telemetryProperties;
            }
        }
    }
}