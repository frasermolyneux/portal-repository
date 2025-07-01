using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.RepositoryWebApi.V1.Extensions
{
    public static class AdminActionTypeExtensions
    {
        public static AdminActionType ToAdminActionType(this int adminActionType)
        {
            return (AdminActionType)adminActionType;
        }

        public static int ToAdminActionTypeInt(this AdminActionType adminActionType)
        {
            return (int)adminActionType;
        }
    }
}
