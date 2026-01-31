using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;

[Obsolete("Use CollectionModel<ChatMessageDto> from MX.Api.Abstractions instead")]
public record ChatMessagesCollectionDto
{
    public List<ChatMessageDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
