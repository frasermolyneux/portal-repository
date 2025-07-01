using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions.V1
{
    public static class AdminActionTypeExtensions
    {
        public static AdminActionType ToAdminActionType(this string adminActionType)
        {
            return Enum.Parse<AdminActionType>(adminActionType);
        }
    }
}
