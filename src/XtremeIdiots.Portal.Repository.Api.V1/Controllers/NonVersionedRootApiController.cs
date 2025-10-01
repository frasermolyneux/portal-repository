using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("")]
public class RootApiController : ControllerBase
{
    [HttpHead]
    [HttpGet]
    [HttpPost]
    [Route("")]
    public IActionResult GetRoot()
    {
        return Ok();
    }
}