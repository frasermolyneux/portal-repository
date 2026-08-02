using System;
using System.Threading.Tasks;

using MX.Api.Abstractions;
using MX.Api.Client.Configuration;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;
using XtremeIdiots.Portal.Repository.Abstractions.Models;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2.Caching
{
    /// <summary>
    /// Default cache policies shipped by the Repository API Client V2 package.
    /// </summary>
    /// <remarks>
    /// V2 currently exposes only <see cref="IApiInfoApi"/> and <see cref="IApiHealthApi"/>.
    /// Both must be treated as never-cache: <c>/info</c> drives deploy version verification and
    /// <c>/health/*</c> must always return live status. There is no V2 resource surface to
    /// register positive cache policies against.
    /// </remarks>
    public static class RepositoryApiCacheDefaults
    {
        /// <summary>Cache defaults for V2 <see cref="IApiInfoApi"/>: never-cache.</summary>
        public static void ConfigureApiInfo(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.NotCached<IApiInfoApi, Task<ApiResult<ApiInfoDto>>>(x => x.GetApiInfo(default));
        }

        /// <summary>Cache defaults for V2 <see cref="IApiHealthApi"/>: never-cache.</summary>
        public static void ConfigureApiHealth(CacheBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.NotCached<IApiHealthApi, Task<ApiResult>>(x => x.CheckHealth(default));
        }
    }
}
