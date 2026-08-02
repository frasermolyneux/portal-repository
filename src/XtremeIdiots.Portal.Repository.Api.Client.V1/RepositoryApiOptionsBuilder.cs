using System;
using System.Reflection;

using MX.Api.Client.Configuration;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    /// <summary>
    /// Builder for Repository API options
    /// </summary>
    public class RepositoryApiOptionsBuilder : ApiClientOptionsBuilder<RepositoryApiClientOptions, RepositoryApiOptionsBuilder>
    {
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
        /// Configures caching for the Repository API client with cross-sub-API scope isolation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="ServiceCollectionExtensions.AddRepositoryApiClient"/> registration
        /// re-invokes the consumer's configuration delegate once per typed sub-API
        /// (<see cref="Abstractions.Interfaces.V1.IAdminActionsApi"/>,
        /// <see cref="Abstractions.Interfaces.V1.IGameServersApi"/>, etc.). MX.Api.Client 2.3.76
        /// scopes each builder to a single typed client, so an expression such as
        /// <c>c.NotCached&lt;IGameServersApi, ...&gt;(x =&gt; x.GetGameServer(...))</c> would throw
        /// <see cref="ArgumentException"/> when replayed against every non-matching typed client.
        /// </para>
        /// <para>
        /// This override captures the consumer's intent once against an unscoped shadow builder,
        /// then replays only the operations whose declaring interface is assignable to the
        /// currently-configured typed client, preserving library-default opt-in and consumer
        /// overrides while eliminating the cross-client crash. Library defaults registered via
        /// <see cref="MX.Api.Client.Extensions.ApiClientExtensions.AddDefaultCachePolicies{TClient}"/>
        /// continue to apply per typed client as designed.
        /// </para>
        /// </remarks>
        /// <param name="configure">The cache policy configuration callback.</param>
        /// <returns>The builder for chaining.</returns>
        public new RepositoryApiOptionsBuilder WithCaching(Action<CacheBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            // Run the consumer's delegate against a fresh, unscoped shadow builder so any
            // expression targeting any sub-API validates only against its own TApi/DeclaringType
            // relationship and never against a mismatched _configuredClientType.
            var shadow = new RepositoryApiOptionsBuilder();
            _ = ((ApiClientOptionsBuilder<RepositoryApiClientOptions, RepositoryApiOptionsBuilder>)shadow)
                .WithCaching(configure);

            var shadowOptions = shadow.Options;

            if (shadowOptions.UseLibraryCacheDefaults)
            {
                _ = base.WithCaching(static c => c.UseLibraryDefaults());
            }

            if (shadowOptions.CachePolicyOperations.Count == 0)
            {
                return this;
            }

            var currentTypedClient = ReadConfiguredClientType(this);

            foreach (var kvp in shadowOptions.CachePolicyOperations)
            {
                var declaringType = kvp.Key.DeclaringType;
                if (declaringType is null)
                {
                    continue;
                }

                if (currentTypedClient is not null && !declaringType.IsAssignableFrom(currentTypedClient))
                {
                    // Expression targets a different sub-API contract than the typed client
                    // currently being configured. Skip - it will be applied when the matching
                    // typed client's builder pass runs.
                    continue;
                }

                ApplyCachePolicyOperation(Options, kvp.Key, kvp.Value);
            }

            return this;
        }

        private static readonly FieldInfo ConfiguredClientTypeField = typeof(ApiClientOptionsBuilder<RepositoryApiClientOptions, RepositoryApiOptionsBuilder>)
            .GetField("_configuredClientType", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "MX.Api.Client contract change: expected private field '_configuredClientType' on ApiClientOptionsBuilder<,> was not found. "
                + "Repository client cache scoping cannot be applied safely without it.");

        private static readonly MethodInfo SetCachePolicyOperationMethod = typeof(ApiClientOptionsBase)
            .GetMethod(
                "SetCachePolicyOperation",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(MethodInfo), typeof(CachePolicyOperation) },
                modifiers: null)
            ?? throw new InvalidOperationException(
                "MX.Api.Client contract change: expected internal method 'ApiClientOptionsBase.SetCachePolicyOperation(MethodInfo, CachePolicyOperation)' was not found. "
                + "Repository client cache scoping cannot be applied safely without it.");

        private static Type? ReadConfiguredClientType(RepositoryApiOptionsBuilder builder)
            => (Type?)ConfiguredClientTypeField.GetValue(builder);

        private static void ApplyCachePolicyOperation(ApiClientOptionsBase options, MethodInfo method, CachePolicyOperation operation)
            => SetCachePolicyOperationMethod.Invoke(options, new object[] { method, operation });
    }
}

