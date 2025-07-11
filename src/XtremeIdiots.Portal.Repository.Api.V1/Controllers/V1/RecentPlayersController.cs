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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class RecentPlayersController : ControllerBase, IRecentPlayersApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public RecentPlayersController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("recent-players")]
        public async Task<IActionResult> GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int? skipEntries, int? takeEntries, RecentPlayersOrder? order)
        {
            if (!skipEntries.HasValue)
                skipEntries = 0;

            if (!takeEntries.HasValue)
                takeEntries = 20;

            if (cutoff.HasValue && cutoff.Value < DateTime.UtcNow.AddHours(-48))
                cutoff = DateTime.UtcNow.AddHours(-48);

            var response = await ((IRecentPlayersApi)this).GetRecentPlayers(gameType, gameServerId, cutoff, filter, skipEntries.Value, takeEntries.Value, order);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<RecentPlayersCollectionDto>> IRecentPlayersApi.GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order)
        {
            var query = context.RecentPlayers.Include(rp => rp.Player).AsQueryable();
            query = ApplyFilter(query, gameType, null, null, null);
            var totalCount = await query.CountAsync();

            query = ApplyFilter(query, gameType, gameServerId, cutoff, filter);
            var filteredCount = await query.CountAsync();

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync();

            var entries = results.Select(rp => mapper.Map<RecentPlayerDto>(rp)).ToList();

            var result = new RecentPlayersCollectionDto
            {
                TotalRecords = totalCount,
                FilteredRecords = filteredCount,
                Entries = entries
            };

            return new ApiResponseDto<RecentPlayersCollectionDto>(HttpStatusCode.OK, result);
        }

        [HttpPost]
        [Route("recent-players")]
        public async Task<IActionResult> CreateRecentPlayers()
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            List<CreateRecentPlayerDto>? createRecentPlayerDtos;
            try
            {
                createRecentPlayerDtos = JsonConvert.DeserializeObject<List<CreateRecentPlayerDto>>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (createRecentPlayerDtos == null || !createRecentPlayerDtos.Any())
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null or did not contain any entries" }).ToHttpResult();

            var response = await ((IRecentPlayersApi)this).CreateRecentPlayers(createRecentPlayerDtos);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IRecentPlayersApi.CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos)
        {
            foreach (var createRecentPlayerDto in createRecentPlayerDtos)
            {
                var recentPlayer = await context.RecentPlayers.SingleOrDefaultAsync(rp => rp.PlayerId == createRecentPlayerDto.PlayerId);

                if (recentPlayer != null)
                {
                    mapper.Map(createRecentPlayerDto, recentPlayer);
                    recentPlayer.Timestamp = DateTime.UtcNow;
                }
                else
                {
                    recentPlayer = mapper.Map<RecentPlayer>(createRecentPlayerDto);
                    recentPlayer.Timestamp = DateTime.UtcNow;

                    await context.RecentPlayers.AddAsync(recentPlayer);
                }
            }

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        private static IQueryable<RecentPlayer> ApplyFilter(IQueryable<RecentPlayer> query, GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(rp => rp.GameType == gameType.Value.ToGameTypeInt()).AsQueryable();

            if (gameServerId.HasValue)
                query = query.Where(rp => rp.GameServerId == gameServerId).AsQueryable();

            if (cutoff.HasValue)
                query = query.Where(rp => rp.Timestamp > cutoff).AsQueryable();

            if (filter.HasValue)
            {
                switch (filter)
                {
                    case RecentPlayersFilter.GeoLocated:
                        query = query.Where(rp => rp.Lat != 0 && rp.Long != 0).AsQueryable();
                        break;
                }
            }

            return query;
        }

        private static IQueryable<RecentPlayer> ApplyOrderAndLimits(IQueryable<RecentPlayer> query, int skipEntries, int takeEntries, RecentPlayersOrder? order)
        {
            if (order.HasValue)
            {
                switch (order)
                {
                    case RecentPlayersOrder.TimestampAsc:
                        query = query.OrderBy(rp => rp.Timestamp).AsQueryable();
                        break;
                    case RecentPlayersOrder.TimestampDesc:
                        query = query.OrderByDescending(rp => rp.Timestamp).AsQueryable();
                        break;
                }
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}
