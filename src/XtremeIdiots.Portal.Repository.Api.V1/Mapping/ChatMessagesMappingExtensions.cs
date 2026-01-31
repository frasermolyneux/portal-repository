using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for ChatMessage entities and DTOs.
    /// </summary>
    public static class ChatMessagesMappingExtensions
    {
        /// <summary>
        /// Maps a ChatMessage entity to a ChatMessageDto.
        /// Uses expand parameter to optionally include shallow Player and GameServer DTOs.
        /// </summary>
        /// <param name="entity">The ChatMessage entity to map from.</param>
        /// <returns>The mapped ChatMessageDto.</returns>
        public static ChatMessageDto ToDto(this ChatMessage entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ChatMessageDto
            {
                ChatMessageId = entity.ChatMessageId,
                GameServerId = entity.GameServerId,
                PlayerId = entity.PlayerId,
                Username = entity.Username ?? string.Empty,
                ChatType = entity.ChatType.ToChatType(),
                Message = entity.Message ?? string.Empty,
                Timestamp = entity.Timestamp,
                Locked = entity.Locked,
                Player = expand && entity.Player is not null ? entity.Player.ToDto(false) : null!,
                GameServer = expand && entity.GameServer is not null ? entity.GameServer.ToDto(false) : null!
            };
        }

        /// <summary>
        /// Maps a CreateChatMessageDto to a ChatMessage entity.
        /// </summary>
        /// <param name="dto">The CreateChatMessageDto to map from.</param>
        /// <returns>The mapped ChatMessage entity.</returns>
        public static ChatMessage ToEntity(this CreateChatMessageDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ChatMessage
            {
                GameServerId = dto.GameServerId,
                PlayerId = dto.PlayerId,
                Username = dto.Username,
                ChatType = (int)dto.ChatType,
                Message = dto.Message,
                Timestamp = dto.Timestamp,
                Locked = false
            };
        }
    }
}