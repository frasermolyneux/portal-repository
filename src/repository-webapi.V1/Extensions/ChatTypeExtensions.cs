using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.RepositoryWebApi.V1.Extensions
{
    public static class ChatTypeExtensions
    {
        public static ChatType ToChatType(this int chatType)
        {
            return (ChatType)chatType;
        }

        public static int ToChatTypeInt(this ChatType chatType)
        {
            return (int)chatType;
        }
    }
}
