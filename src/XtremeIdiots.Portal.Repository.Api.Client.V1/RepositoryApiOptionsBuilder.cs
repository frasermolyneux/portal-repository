using System;

using MX.Api.Client.Configuration;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    /// <summary>
    /// Builder for Repository API options
    /// </summary>
    public class RepositoryApiOptionsBuilder : ApiClientOptionsBuilder<RepositoryApiClientOptions, RepositoryApiOptionsBuilder>
    {
        /// <summary>
        /// Cache configuration delegate captured from a consumer <see cref="WithCaching(Action{CacheBuilder})"/> call.
        /// </summary>
        /// <remarks>
        /// Applied per typed sub-API by <see cref="ServiceCollectionExtensions.AddRepositoryApiClient"/> via a single
        /// <see cref="SharedCacheConfiguration"/> instance and <c>WithSharedCaching</c>, which scopes operations to
        /// the currently-configured typed client. See also <see cref="ServiceCollectionExtensions"/> for orchestration.
        /// </remarks>
        internal Action<CacheBuilder>? CapturedCacheConfigure { get; private set; }

        /// <summary>
        /// Creates a new instance of the RepositoryApiOptionsBuilder
        /// </summary>
        public RepositoryApiOptionsBuilder() : base() { }

        /// <summary>
        /// Configures the default page size for repository operations
        /// </summary>
        /// <param name="pageSize">The page size</param>
        /// <returns>The builder for chaining</returns>
        public RepositoryApiOptionsBuilder WithDefaultPageSize(int pageSize)
        {
            Options.DefaultPageSize = pageSize;
            return this;
        }

        /// <summary>
        /// Configures whether to enable caching
        /// </summary>
        /// <param name="enableCaching">Whether to enable caching</param>
        /// <returns>The builder for chaining</returns>
        public RepositoryApiOptionsBuilder WithCaching(bool enableCaching = true)
        {
            Options.EnableCaching = enableCaching;
            return this;
        }

        /// <summary>
        /// Captures a caching configuration delegate for cross-sub-API application.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="ServiceCollectionExtensions.AddRepositoryApiClient"/> re-invokes the consumer's configuration
        /// delegate once per typed sub-API (<see cref="Abstractions.Interfaces.V1.IAdminActionsApi"/>,
        /// <see cref="Abstractions.Interfaces.V1.IGameServersApi"/>, etc.). The base
        /// <see cref="ApiClientOptionsBuilder{TOptions,TBuilder}.WithCaching(Action{CacheBuilder})"/> scopes each call to
        /// a single typed client, so an expression such as
        /// <c>c.NotCached&lt;IGameServersApi, ...&gt;(x =&gt; x.GetGameServer(...))</c> would throw
        /// <see cref="ArgumentException"/> when replayed against every non-matching typed client.
        /// </para>
        /// <para>
        /// This override captures the delegate on <see cref="CapturedCacheConfigure"/> without applying it.
        /// <see cref="ServiceCollectionExtensions.AddRepositoryApiClient"/> creates a single
        /// <see cref="SharedCacheConfiguration"/> from the captured delegate and applies it to every typed client via
        /// <see cref="ApiClientOptionsBuilder{TOptions,TBuilder}.WithSharedCaching(SharedCacheConfiguration)"/>, which
        /// natively scopes operations to the current typed client and skips unrelated siblings. Library defaults registered
        /// via <see cref="MX.Api.Client.Extensions.ApiClientExtensions.AddDefaultCachePolicies{TClient}"/> continue to
        /// apply per typed client as designed.
        /// </para>
        /// </remarks>
        /// <param name="configure">The cache policy configuration callback.</param>
        /// <returns>The builder for chaining.</returns>
        public new RepositoryApiOptionsBuilder WithCaching(Action<CacheBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            CapturedCacheConfigure = configure;
            return this;
        }
    }
}
