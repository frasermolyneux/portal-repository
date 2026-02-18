using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V2;

[ApiController]
[AllowAnonymous]
[ApiVersion("2.0")]
[Route("v{version:apiVersion}/info")]
public class ApiInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult GetInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "unknown";

        return Ok(new
        {
            Version = informationalVersion,
            BuildVersion = informationalVersion.Split('+')[0],
            AssemblyVersion = assemblyVersion
        });
    }
}
