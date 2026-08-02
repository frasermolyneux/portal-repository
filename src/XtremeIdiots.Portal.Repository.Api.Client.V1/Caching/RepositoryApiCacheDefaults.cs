using System;
using System.Threading.Tasks;

using MX.Api.Abstractions;
using MX.Api.Client.Configuration;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1.Caching
{
    /// <summary>
    /// Default cache policies shipped by the Repository API Client V1 package.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MX.Api.Client 2.3.76 registers library defaults per typed API client interface via
    /// <c>AddDefaultCachePolicies&lt;TClient&gt;(...)</c>. Because each sub-API
    /// (<see cref="IGameServersApi"/>, <see cref="IMapsApi"/>, etc.) is registered as its own
    /// typed client through <c>AddTypedApiClient&lt;TClient, ...&gt;</c>, defaults must be
    /// scoped to the same sub-API interface used when the typed client is registered.
    /// </para>
    /// <para>
    /// Policy set:
    /// <list type="bullet">
    ///   <item><description>GET single game server → 60 seconds in-memory.</description></item>
    ///   <item><description>GET game server list → 60 seconds in-memory.</description></item>
    ///   <item><description>GET map by id and GET map by (game type, name) → 10 minutes in-memory.</description></item>
    ///   <item><description>GET maps list → 10 minutes in-memory.</description></item>
    ///   <item><description>All mutating endpoints, user-profile / claims surfaces, and info / health probes → <see cref="CacheBuilder.NotCached{TApi,TResult}"/> to make intent explicit.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// MX.Api.Client 2.3.76 does not support parameter-aware dynamic tag expansion on
    /// client-side cache policies, so no <c>gameserver:{id}</c>-style tags are attached
    /// here — those live on the server-side <see cref="MX.Caching.Abstractions.IMxCache"/>
    /// path where the actual identifier is bound at invocation time.
    /// </para>
    /// </remarks>
    public static class RepositoryApiCacheDefaults
    {
        /// <summary>Default TTL for game-server reads.</summary>
        public static readonly TimeSpan GameServerTtl = TimeSpan.FromSeconds(60);

        /// <summary>Default TTL for map reads.</summary>
        public static readonly TimeSpan MapTtl = TimeSpan.FromMinutes(10);

        /// <summary>Cache defaults for <see cref="IGameServersApi"/>.</summary>
        public static void ConfigureGameServers(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder
                .InMemory<IGameServersApi, Task<ApiResult<GameServerDto>>>(
                    x => x.GetGameServer(default, default),
                    GameServerTtl)
                .InMemory<IGameServersApi, Task<ApiResult<CollectionModel<GameServerDto>>>>(
                    x => x.GetGameServers(default, default, default, default, default, default, default),
                    GameServerTtl);

            builder
                .NotCached<IGameServersApi, Task<ApiResult>>(x => x.CreateGameServer(default!, default))
                .NotCached<IGameServersApi, Task<ApiResult>>(x => x.CreateGameServers(default!, default))
                .NotCached<IGameServersApi, Task<ApiResult>>(x => x.UpdateGameServer(default!, default))
                .NotCached<IGameServersApi, Task<ApiResult>>(x => x.UpdateGameServerOrder(default!, default))
                .NotCached<IGameServersApi, Task<ApiResult>>(x => x.DeleteGameServer(default, default));
        }

        /// <summary>Cache defaults for <see cref="IMapsApi"/>.</summary>
        public static void ConfigureMaps(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder
                .InMemory<IMapsApi, Task<ApiResult<MapDto>>>(
                    x => x.GetMap(Guid.Empty, default),
                    MapTtl)
                .InMemory<IMapsApi, Task<ApiResult<MapDto>>>(
                    x => x.GetMap(GameType.Unknown, string.Empty, default),
                    MapTtl)
                .InMemory<IMapsApi, Task<ApiResult<CollectionModel<MapDto>>>>(
                    x => x.GetMaps(default, default, default, default, default, default, default, default),
                    MapTtl);

            builder
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.CreateMap(default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.CreateMaps(default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.UpdateMap(default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.UpdateMaps(default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.DeleteMap(default, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.RebuildMapPopularity(default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.UpsertMapVote(default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.UpsertMapVotes(default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.UpdateMapImage(default, default!, default))
                .NotCached<IMapsApi, Task<ApiResult>>(x => x.ClearMapImage(default, default));
        }

        /// <summary>Cache defaults for <see cref="IUserProfileApi"/>: everything NotCached.</summary>
        public static void ConfigureUserProfile(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder
                .NotCached<IUserProfileApi, Task<ApiResult<UserProfileDto>>>(x => x.GetUserProfile(default, default))
                .NotCached<IUserProfileApi, Task<ApiResult<UserProfileDto>>>(x => x.GetUserProfileByIdentityId(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult<UserProfileDto>>>(x => x.GetUserProfileByXtremeIdiotsId(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult<UserProfileDto>>>(x => x.GetUserProfileByDemoAuthKey(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult<CollectionModel<UserProfileDto>>>>(
                    x => x.GetUserProfiles(default, default, default, default, default, default))
                .NotCached<IUserProfileApi, Task<ApiResult<CollectionModel<PermissionReportEntryDto>>>>(
                    x => x.GetPermissionsReport(default, default, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.CreateUserProfile(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.CreateUserProfiles(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.UpdateUserProfile(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.UpdateUserProfiles(default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.CreateUserProfileClaim(default, default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.SetUserProfileClaims(default, default!, default))
                .NotCached<IUserProfileApi, Task<ApiResult>>(x => x.DeleteUserProfileClaim(default, default, default));
        }

        /// <summary>Cache defaults for <see cref="IApiInfoApi"/>: never-cache.</summary>
        public static void ConfigureApiInfo(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.NotCached<IApiInfoApi, Task<ApiResult<ApiInfoDto>>>(x => x.GetApiInfo(default));
        }

        /// <summary>Cache defaults for <see cref="IApiHealthApi"/>: never-cache.</summary>
        public static void ConfigureApiHealth(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.NotCached<IApiHealthApi, Task<ApiResult>>(x => x.CheckHealth(default));
        }
    }
}
