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
        /// NOTE: Player and GameServer navigation properties will be null to avoid circular dependencies.
        /// </summary>
        /// <param name="entity">The ChatMessage entity to map from.</param>
        /// <returns>The mapped ChatMessageDto.</returns>
        public static ChatMessageDto ToDto(this ChatMessage entity)
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
                Player = null!, // Set separately to avoid circular dependencies
                GameServer = null! // Set separately to avoid circular dependencies
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