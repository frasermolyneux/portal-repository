using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class LivePlayersController : Controller, ILivePlayersApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public LivePlayersController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("live-players")]
        public async Task<IActionResult> GetLivePlayers(GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter, int? skipEntries, int? takeEntries, LivePlayersOrder? order)
        {
            if (!skipEntries.HasValue)
                skipEntries = 0;

            if (!takeEntries.HasValue)
                takeEntries = 20;

            var response = await ((ILivePlayersApi)this).GetLivePlayers(gameType, gameServerId, filter, skipEntries.Value, takeEntries.Value, order);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<LivePlayersCollectionDto>> ILivePlayersApi.GetLivePlayers(GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter, int skipEntries, int takeEntries, LivePlayersOrder? order)
        {
            var query = context.LivePlayers.Include(lp => lp.Player).AsQueryable();
            query = ApplyFilter(query, gameType, null, null);
            var totalCount = await query.CountAsync();

            query = ApplyFilter(query, gameType, gameServerId, filter);
            var filteredCount = await query.CountAsync();

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync();

            var entries = results.Select(lp => mapper.Map<LivePlayerDto>(lp)).ToList();

            var result = new LivePlayersCollectionDto
            {
                TotalRecords = totalCount,
                FilteredRecords = filteredCount,
                Entries = entries
            };

            return new ApiResponseDto<LivePlayersCollectionDto>(HttpStatusCode.OK, result);
        }

        [HttpPost]
        [Route("live-players/{gameServerId}")]
        public async Task<IActionResult> SetLivePlayersForGameServer(Guid gameServerId)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            List<CreateLivePlayerDto>? createLivePlayerDtos;
            try
            {
                createLivePlayerDtos = JsonConvert.DeserializeObject<List<CreateLivePlayerDto>>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (createLivePlayerDtos == null)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

            var response = await ((ILivePlayersApi)this).SetLivePlayersForGameServer(gameServerId, createLivePlayerDtos);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> ILivePlayersApi.SetLivePlayersForGameServer(Guid gameServerId, List<CreateLivePlayerDto> createLivePlayerDtos)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[LivePlayers] WHERE [GameServerId] = {gameServerId}");

            var livePlayers = createLivePlayerDtos.Select(lp => mapper.Map<LivePlayer>(lp)).ToList();

            await context.LivePlayers.AddRangeAsync(livePlayers);
            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        private IQueryable<LivePlayer> ApplyFilter(IQueryable<LivePlayer> query, GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(lp => lp.GameType == gameType.Value.ToGameTypeInt()).AsQueryable();

            if (gameServerId.HasValue)
                query = query.Where(lp => lp.GameServerId == gameServerId).AsQueryable();

            if (filter != null)
            {
                switch (filter)
                {
                    case LivePlayerFilter.GeoLocated:
                        query = query.Where(lp => lp.Lat != null && lp.Long != null).AsQueryable();
                        break;
                }
            }

            return query;
        }

        private IQueryable<LivePlayer> ApplyOrderAndLimits(IQueryable<LivePlayer> query, int skipEntries, int takeEntries, LivePlayersOrder? order)
        {
            if (order.HasValue)
            {
                switch (order)
                {
                    case LivePlayersOrder.ScoreAsc:
                        query = query.OrderBy(rp => rp.Score).AsQueryable();
                        break;
                    case LivePlayersOrder.ScoreDesc:
                        query = query.OrderByDescending(rp => rp.Score).AsQueryable();
                        break;
                }
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}
