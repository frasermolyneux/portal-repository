using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages
{
    public record CreateChatMessageDto : IDto
    {
        public CreateChatMessageDto(Guid gameServerId, Guid playerId, ChatType chatType, string username, string message, DateTime timestamp)
        {
            GameServerId = gameServerId;
            PlayerId = playerId;
            ChatType = chatType;
            Username = username;
            Message = message;
            Timestamp = timestamp;
        }

        [JsonProperty]
        public Guid GameServerId { get; private set; }

        [JsonProperty]
        public Guid PlayerId { get; private set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public ChatType ChatType { get; private set; }

        [JsonProperty]
        public string Username { get; private set; }

        [JsonProperty]
        public string Message { get; private set; }

        [JsonProperty]
        public DateTime Timestamp { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameServerId), GameServerId.ToString() },
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(ChatType), ChatType.ToString() }
        };
    }
}
