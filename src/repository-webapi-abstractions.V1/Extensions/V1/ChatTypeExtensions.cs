using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions.V1
{
    public static class ChatTypeExtensions
    {
        public static ChatType ToChatType(this string chatType)
        {
            return Enum.Parse<ChatType>(chatType);
        }
    }
}
