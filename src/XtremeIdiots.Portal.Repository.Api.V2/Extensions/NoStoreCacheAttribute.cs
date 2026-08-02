using Microsoft.AspNetCore.Mvc.Filters;

namespace XtremeIdiots.Portal.Repository.Api.V2.Extensions
{
    /// <summary>
    /// Action filter that stamps <c>Cache-Control: no-store, no-cache</c>, <c>Pragma: no-cache</c>
    /// and <c>Expires: 0</c> on the response. Applied to endpoints that must never be served
    /// from any cache — most importantly <c>/info</c> (deploy version verification) and
    /// <c>/health/*</c> (live status).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class NoStoreCacheAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var headers = context.HttpContext.Response.Headers;
            headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            headers.Pragma = "no-cache";
            headers.Expires = "0";
        }
    }
}
