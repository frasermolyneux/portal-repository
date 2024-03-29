﻿using System.Net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
public class DataMaintenanceController : ControllerBase, IDataMaintenanceApi
{
    private readonly PortalDbContext context;

    public DataMaintenanceController(PortalDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpDelete]
    [Route("data-maintenance/prune-chat-messages")]
    public async Task<IActionResult> PruneChatMessages()
    {
        var response = await ((IDataMaintenanceApi)this).PruneChatMessages();

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IDataMaintenanceApi.PruneChatMessages()
    {
        await context.Database.ExecuteSqlRawAsync($"DELETE FROM [dbo].[{nameof(context.ChatMessages)}] WHERE [Timestamp] < CAST('{DateTime.UtcNow.AddMonths(-6):yyyy-MM-dd} 12:00:00' AS date)");
        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpDelete]
    [Route("data-maintenance/prune-game-server-events")]
    public async Task<IActionResult> PruneGameServerEvents()
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerEvents();

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IDataMaintenanceApi.PruneGameServerEvents()
    {
        await context.Database.ExecuteSqlRawAsync($"DELETE FROM [dbo].[{nameof(context.GameServerEvents)}] WHERE [Timestamp] < CAST('{DateTime.UtcNow.AddMonths(-6):yyyy-MM-dd} 12:00:00' AS date)");
        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpDelete]
    [Route("data-maintenance/prune-game-server-stats")]
    public async Task<IActionResult> PruneGameServerStats()
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerStats();

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IDataMaintenanceApi.PruneGameServerStats()
    {
        await context.Database.ExecuteSqlRawAsync($"DELETE FROM [dbo].[{nameof(context.GameServerStats)}] WHERE [Timestamp] < CAST('{DateTime.UtcNow.AddMonths(-6):yyyy-MM-dd} 12:00:00' AS date)");
        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpDelete]
    [Route("data-maintenance/prune-recent-players")]
    public async Task<IActionResult> PruneRecentPlayers()
    {
        var response = await ((IDataMaintenanceApi)this).PruneRecentPlayers();

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IDataMaintenanceApi.PruneRecentPlayers()
    {
        await context.Database.ExecuteSqlRawAsync($"DELETE FROM [dbo].[{nameof(context.RecentPlayers)}] WHERE [Timestamp] < CAST('{DateTime.UtcNow.AddDays(-7):yyyy-MM-dd} 12:00:00' AS date)");
        return new ApiResponseDto(HttpStatusCode.OK);
    }
}