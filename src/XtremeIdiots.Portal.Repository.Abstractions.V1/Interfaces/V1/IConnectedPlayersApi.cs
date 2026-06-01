using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IConnectedPlayersApi
    {
        Task<ApiResult> CreateConnectedPlayerLink(CreateConnectedPlayerLinkDto dto, CancellationToken cancellationToken = default);

        Task<ApiResult<ConnectedPlayerActivationCodeDto>> ActivateConnectedPlayerActivationCode(
            ActivateConnectedPlayerActivationCodeDto dto,
            CancellationToken cancellationToken = default);

        Task<ApiResult<ConnectedPlayerActivationCodeDto>> GetActiveConnectedPlayerActivationCode(
            Guid userProfileId,
            CancellationToken cancellationToken = default);

        Task<ApiResult<IssueConnectedPlayerRegistrationTokenResultDto>> IssueConnectedPlayerRegistrationToken(
            IssueConnectedPlayerRegistrationTokenDto dto,
            CancellationToken cancellationToken = default);

        Task<ApiResult<ConnectedPlayerDto>> VerifyConnectedPlayerRegistrationToken(
            VerifyConnectedPlayerRegistrationTokenDto dto,
            CancellationToken cancellationToken = default);

        Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> GetConnectedPlayersByUserProfile(
            Guid userProfileId,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken = default);

        Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> GetConnectedPlayers(
            Guid? playerId,
            Guid? userProfileId,
            GameType? gameType,
            bool? isActive,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken = default);

        Task<ApiResult> ForceUnlinkConnectedPlayer(
            Guid connectedPlayerProfileId,
            ForceUnlinkConnectedPlayerDto dto,
            CancellationToken cancellationToken = default);
    }
}
