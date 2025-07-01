using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Extensions
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
