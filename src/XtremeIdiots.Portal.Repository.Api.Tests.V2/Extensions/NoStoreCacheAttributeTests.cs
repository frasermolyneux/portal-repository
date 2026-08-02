using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

using XtremeIdiots.Portal.Repository.Api.V2.Extensions;

using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V2.Extensions
{
    /// <summary>
    /// Verifies the V2 <see cref="NoStoreCacheAttribute"/> stamps deterministic never-cache
    /// headers on responses.
    /// </summary>
    public class NoStoreCacheAttributeTests
    {
        [Fact]
        public void OnActionExecuted_SetsCacheControlNoStore()
        {
            var (attribute, context) = CreateExecutedContext();

            attribute.OnActionExecuted(context);

            var headers = context.HttpContext.Response.Headers;
            Assert.Equal("no-store, no-cache, must-revalidate, max-age=0", headers.CacheControl.ToString());
            Assert.Equal("no-cache", headers.Pragma.ToString());
            Assert.Equal("0", headers.Expires.ToString());
        }

        [Fact]
        public void AppliedToInfoAndHealthControllers()
        {
            var infoType = typeof(RepositoryWebApi.Controllers.V2.ApiInfoController);
            var healthType = typeof(RepositoryWebApi.Controllers.V2.HealthController);

            Assert.NotEmpty(infoType.GetCustomAttributes(typeof(NoStoreCacheAttribute), inherit: true));
            Assert.NotEmpty(healthType.GetCustomAttributes(typeof(NoStoreCacheAttribute), inherit: true));
        }

        private static (NoStoreCacheAttribute attribute, ActionExecutedContext context) CreateExecutedContext()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: new object());
            return (new NoStoreCacheAttribute(), context);
        }
    }
}
