using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using MX.Api.Client.Caching;
using MX.Api.Client.Configuration;
using MX.Api.Client.Extensions;
using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;
using XtremeIdiots.Portal.Repository.Api.Client.V2.Caching;

using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
{
    /// <summary>
    /// Verifies the V2 API client's default cache policy set. V2 only exposes info and health
    /// probes so every default must be <see cref="CachePolicy.NotCached"/>.
    /// </summary>
    public class RepositoryApiCacheDefaultsTests
    {
        private static IReadOnlyDictionary<MethodInfo, CachePolicy> PoliciesFor<TApi>(Action<CacheBuilder> configure)
            where TApi : class
        {
            var services = new ServiceCollection();
            services.AddDefaultCachePolicies<TApi>(configure);
            using var sp = services.BuildServiceProvider();
            var defaults = sp.GetRequiredService<DefaultCachePolicies<TApi>>();
            return defaults.Policies;
        }

        [Fact]
        public void ConfigureApiInfo_GetApiInfoIsNotCached()
        {
            var policies = PoliciesFor<IApiInfoApi>(RepositoryApiCacheDefaults.ConfigureApiInfo);

            var policy = Assert.Single(policies);
            Assert.Equal(nameof(IApiInfoApi.GetApiInfo), policy.Key.Name);
            Assert.Same(CachePolicy.NotCached, policy.Value);
        }

        [Fact]
        public void ConfigureApiHealth_CheckHealthIsNotCached()
        {
            var policies = PoliciesFor<IApiHealthApi>(RepositoryApiCacheDefaults.ConfigureApiHealth);

            var policy = Assert.Single(policies);
            Assert.Equal(nameof(IApiHealthApi.CheckHealth), policy.Key.Name);
            Assert.Same(CachePolicy.NotCached, policy.Value);
        }
    }
}
