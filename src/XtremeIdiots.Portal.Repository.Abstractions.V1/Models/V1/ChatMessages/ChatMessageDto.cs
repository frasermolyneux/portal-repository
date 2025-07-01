using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages
{
        public record ChatMessageDto : IDto
        {
                [JsonProperty]
                public Guid ChatMessageId { get; internal set; }

                [JsonProperty]
                public Guid GameServerId { get; internal set; }

                [JsonProperty]
                public Guid PlayerId { get; internal set; }

                [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                public string Username { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

                [JsonProperty]
                [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
                public ChatType ChatType { get; internal set; }

                [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                public string Message { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

                [JsonProperty]
                public DateTime Timestamp { get; internal set; }

                [JsonProperty]
                public bool Locked { get; internal set; }

                [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                public PlayerDto Player { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

                [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                public GameServerDto GameServer { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

                [JsonIgnore]
                public Dictionary<string, string> TelemetryProperties
                {
                        get
                        {
                                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(ChatMessageId), ChatMessageId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(ChatType), ChatType.ToString() },
                    { nameof(Locked), Locked.ToString() }
                };

                                if (GameServer is not null)
                                        telemetryProperties.AddAdditionalProperties(GameServer.TelemetryProperties);

                                if (Player is not null)
                                        telemetryProperties.AddAdditionalProperties(Player.TelemetryProperties);

                                return telemetryProperties;
                        }
                }
        }
}
