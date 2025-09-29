using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class PlayersController : ControllerBase, IPlayersApi
{
    private readonly PortalDbContext context;
    private readonly IMemoryCache _memoryCache;

    public PlayersController(
        PortalDbContext context,
        IMemoryCache memoryCache)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this._memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <summary>
    /// Retrieves a specific player by their unique identifier.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player to retrieve.</param>
    /// <param name="playerEntityOptions">Options specifying which related entities to include in the response.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The player details if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("players/{playerId:guid}")]
    [ProducesResponseType<PlayerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayer(playerId, playerEntityOptions);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a specific player by their unique identifier.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player to retrieve.</param>
    /// <param name="playerEntityOptions">Options specifying which related entities to include in the response.</param>
    /// <returns>An API result containing the player details if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<PlayerDto>> IPlayersApi.GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
    {
        var player = await context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId);

        if (player == null)
            return new ApiResult<PlayerDto>(HttpStatusCode.NotFound);

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            player.PlayerAliases = await context.PlayerAliases
                .AsNoTracking()
                .Where(pa => pa.PlayerId == player.PlayerId)
                .OrderByDescending(pa => pa.LastUsed)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
            player.PlayerIpAddresses = await context.PlayerIpAddresses
                .AsNoTracking()
                .Where(pip => pip.PlayerId == player.PlayerId)
                .OrderByDescending(pip => pip.LastUsed)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
            player.AdminActions = await context.AdminActions
                .AsNoTracking()
                .Include(aa => aa.UserProfile)
                .Where(aa => aa.PlayerId == player.PlayerId)
                .OrderByDescending(aa => aa.Created)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.ProtectedNames))
            player.ProtectedNames = await context.ProtectedNames
                .AsNoTracking()
                .Include(pn => pn.CreatedByUserProfile)
                .Where(pn => pn.PlayerId == player.PlayerId)
                .OrderByDescending(pn => pn.CreatedOn)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags))
            player.PlayerTags = await context.PlayerTags
                .AsNoTracking()
                .Include(pt => pt.Tag)
                .Where(pt => pt.PlayerId == player.PlayerId)
                .ToListAsync();

        var result = player.ToDto();

        // Populate navigation properties based on what was loaded
        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases) && player.PlayerAliases != null)
            result.PlayerAliases = player.PlayerAliases.Select(pa => pa.ToAliasDto()).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses) && player.PlayerIpAddresses != null)
            result.PlayerIpAddresses = player.PlayerIpAddresses.Select(pip => pip.ToIpAddressDto()).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.ProtectedNames) && player.ProtectedNames != null)
            result.ProtectedNames = player.ProtectedNames.Select(pn => pn.ToProtectedNameDto()).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags) && player.PlayerTags != null)
            result.Tags = player.PlayerTags.Select(pt => pt.ToDto()).ToList();

        // Note: AdminActions and Reports mappings are temporarily skipped to avoid circular dependencies
        // TODO: Will need to implement these mappings separately

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.RelatedPlayers))
        {
            var playerIpAddresses = await context.PlayerIpAddresses
                .AsNoTracking()
                .Include(ip => ip.Player)
                .Where(ip => ip.Address == player.IpAddress && ip.PlayerId != player.PlayerId)
                .ToListAsync();

            result.RelatedPlayers = playerIpAddresses.Select(pip => pip.ToRelatedPlayerDto()).ToList();
        }

        return new ApiResponse<PlayerDto>(result).ToApiResult();
    }

    /// <summary>
    /// Checks if a player exists by game type and GUID without returning the player data.
    /// </summary>
    /// <param name="gameType">The game type to search for.</param>
    /// <param name="guid">The GUID of the player to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>200 OK if the player exists; otherwise, 404 Not Found.</returns>
    [HttpHead("players/by-game-type/{gameType}/{guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HeadPlayerByGameType(GameType gameType, string guid, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).HeadPlayerByGameType(gameType, guid);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Checks if a player exists by game type and GUID without returning the player data.
    /// </summary>
    /// <param name="gameType">The game type to search for.</param>
    /// <param name="guid">The GUID of the player to check.</param>
    /// <returns>An API result indicating whether the player exists.</returns>
    async Task<ApiResult> IPlayersApi.HeadPlayerByGameType(GameType gameType, string guid)
    {
        var playerExists = await context.Players
            .AsNoTracking()
            .AnyAsync(p => p.GameType == gameType.ToGameTypeInt() && p.Guid == guid);

        if (!playerExists)
            return new ApiResult(HttpStatusCode.NotFound);

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Retrieves a specific player by game type and GUID.
    /// </summary>
    /// <param name="gameType">The game type to search for.</param>
    /// <param name="guid">The GUID of the player to retrieve.</param>
    /// <param name="playerEntityOptions">Options specifying which related entities to include in the response.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The player details if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("players/by-game-type/{gameType}/{guid}")]
    [ProducesResponseType<PlayerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayerByGameType(gameType, guid, playerEntityOptions);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a specific player by game type and GUID.
    /// </summary>
    /// <param name="gameType">The game type to search for.</param>
    /// <param name="guid">The GUID of the player to retrieve.</param>
    /// <param name="playerEntityOptions">Options specifying which related entities to include in the response.</param>
    /// <returns>An API result containing the player details if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<PlayerDto>> IPlayersApi.GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
    {
        var player = await context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameType == gameType.ToGameTypeInt() && p.Guid == guid);

        if (player == null)
            return new ApiResult<PlayerDto>(HttpStatusCode.NotFound);

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            player.PlayerAliases = await context.PlayerAliases
                .AsNoTracking()
                .Where(pa => pa.PlayerId == player.PlayerId)
                .OrderByDescending(pa => pa.LastUsed)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
            player.PlayerIpAddresses = await context.PlayerIpAddresses
                .AsNoTracking()
                .Where(pip => pip.PlayerId == player.PlayerId)
                .OrderByDescending(pip => pip.LastUsed)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
            player.AdminActions = await context.AdminActions
                .AsNoTracking()
                .Include(aa => aa.UserProfile)
                .Where(aa => aa.PlayerId == player.PlayerId)
                .OrderByDescending(aa => aa.Created)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.ProtectedNames))
            player.ProtectedNames = await context.ProtectedNames
                .AsNoTracking()
                .Include(pn => pn.CreatedByUserProfile)
                .Where(pn => pn.PlayerId == player.PlayerId)
                .OrderByDescending(pn => pn.CreatedOn)
                .ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags))
            player.PlayerTags = await context.PlayerTags
                .AsNoTracking()
                .Include(pt => pt.Tag)
                .Where(pt => pt.PlayerId == player.PlayerId)
                .ToListAsync();

        var result = player.ToDto();

        // Populate navigation properties based on what was loaded
        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases) && player.PlayerAliases != null)
            result.PlayerAliases = player.PlayerAliases.Select(pa => pa.ToAliasDto()).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses) && player.PlayerIpAddresses != null)
            result.PlayerIpAddresses = player.PlayerIpAddresses.Select(pip => pip.ToIpAddressDto()).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.ProtectedNames) && player.ProtectedNames != null)
            result.ProtectedNames = player.ProtectedNames.Select(pn => pn.ToProtectedNameDto()).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags) && player.PlayerTags != null)
            result.Tags = player.PlayerTags.Select(pt => pt.ToDto()).ToList();

        // Note: AdminActions and Reports mappings are temporarily skipped to avoid circular dependencies
        // TODO: Will need to implement these mappings separately

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.RelatedPlayers))
        {
            var playerIpAddresses = await context.PlayerIpAddresses
                .AsNoTracking()
                .Include(ip => ip.Player)
                .Where(ip => ip.Address == player.IpAddress && ip.PlayerId != player.PlayerId)
                .ToListAsync();

            result.RelatedPlayers = playerIpAddresses.Select(pip => pip.ToRelatedPlayerDto()).ToList();
        }

        // Map tags to DTO if requested  
        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags))
            result.Tags = player.PlayerTags?.Select(pt => pt.ToDto()).ToList() ?? new List<PlayerTagDto>();

        return new ApiResponse<PlayerDto>(result).ToApiResult();
    }

    /// <summary>
    /// Retrieves a paginated list of players with optional filtering and sorting.
    /// </summary>
    /// <param name="gameType">Optional filter by game type.</param>
    /// <param name="filter">Optional filter type to apply.</param>
    /// <param name="filterString">Optional filter string to search for.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
    /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
    /// <param name="order">Optional ordering criteria for results.</param>
    /// <param name="playerEntityOptions">Options specifying which related entities to include in the response.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A paginated collection of players.</returns>
    [HttpGet("players")]
    [ProducesResponseType<CollectionModel<PlayerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayers(
        [FromQuery] GameType? gameType,
        [FromQuery] PlayersFilter? filter,
        [FromQuery] string? filterString,
        [FromQuery] int? skipEntries,
        [FromQuery] int? takeEntries,
        [FromQuery] PlayersOrder? order,
        [FromQuery] PlayerEntityOptions playerEntityOptions,
        CancellationToken cancellationToken = default)
    {
        var skipValue = skipEntries ?? 0;
        var takeValue = takeEntries ?? 20;

        var response = await ((IPlayersApi)this).GetPlayers(gameType, filter, filterString, skipValue, takeValue, order, playerEntityOptions);

        return response.ToHttpResult();
    }
    /// <summary>
    /// Retrieves a paginated list of players with optional filtering and sorting.
    /// </summary>
    /// <param name="gameType">Optional filter by game type.</param>
    /// <param name="filter">Optional filter type to apply.</param>
    /// <param name="filterString">Optional filter string to search for.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination.</param>
    /// <param name="takeEntries">Number of entries to take for pagination.</param>
    /// <param name="order">Optional ordering criteria for results.</param>
    /// <param name="playerEntityOptions">Options specifying which related entities to include in the response.</param>
    /// <returns>An API result containing a paginated collection of players.</returns>
    async Task<ApiResult<CollectionModel<PlayerDto>>> IPlayersApi.GetPlayers(
        GameType? gameType,
        PlayersFilter? filter,
        string? filterString,
        int skipEntries,
        int takeEntries,
        PlayersOrder? order,
        PlayerEntityOptions playerEntityOptions)
    {
        // Calculate total count before applying filters for better performance
        var cacheKey = $"PlayerCount_{gameType}";
        int totalCount;

        if (!_memoryCache.TryGetValue(cacheKey, out totalCount))
        {
            var countQuery = context.Players.AsNoTracking();
            if (gameType.HasValue)
                countQuery = countQuery.Where(p => p.GameType == gameType.Value.ToGameTypeInt());

            totalCount = await countQuery.CountAsync();

            // Cache the count for 10 minutes
            _memoryCache.Set(cacheKey, totalCount, TimeSpan.FromMinutes(10));
        }

        // Start building the query for filtered results
        var query = context.Players.AsNoTracking();

        // Apply filters for the specific search
        query = ApplyFilter(query, gameType, filter, filterString);

        // Calculate filtered count before applying ordering and pagination
        var filteredCount = await query.CountAsync();

        // Apply ordering and pagination
        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);

        // Create a list to hold our results
        List<Player> results;

        // Optimize the query based on what related data is needed
        if (playerEntityOptions != PlayerEntityOptions.None)
        {
            // For the main query, load player data with related entities as needed
            if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            {
                results = await query
                    .Select(p => new Player
                    {
                        PlayerId = p.PlayerId,
                        GameType = p.GameType,
                        Username = p.Username,
                        Guid = p.Guid,
                        FirstSeen = p.FirstSeen,
                        LastSeen = p.LastSeen,
                        IpAddress = p.IpAddress,
                        // Preload top 10 most recent aliases
                        PlayerAliases = p.PlayerAliases
                            .OrderByDescending(a => a.LastUsed)
                            .Take(10)
                            .ToList()
                    })
                    .ToListAsync();
            }
            else
            {
                results = await query.ToListAsync();
            }

            // Extract player IDs for use in subsequent queries
            var playerIds = results.Select(p => p.PlayerId).ToList();

            // Use batch loading for related data when needed
            if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
            {
                var playerIdToIpAddresses = await context.PlayerIpAddresses
                    .AsNoTracking()
                    .Where(ip => ip.PlayerId != null && playerIds.Contains(ip.PlayerId.Value))
                    .GroupBy(ip => ip.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key!.Value,
                        IpAddresses = g.OrderByDescending(ip => ip.LastUsed)
                            .Take(10)
                            .ToList()
                    })
                    .ToDictionaryAsync(x => x.PlayerId, x => x.IpAddresses);

                foreach (var player in results)
                {
                    if (playerIdToIpAddresses.TryGetValue(player.PlayerId, out var ipAddresses))
                    {
                        player.PlayerIpAddresses = ipAddresses;
                    }
                    else
                    {
                        player.PlayerIpAddresses = new List<PlayerIpAddress>();
                    }
                }
            }

            if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
            {
                var playerIdToAdminActions = await context.AdminActions
                    .AsNoTracking()
                    .Where(aa => playerIds.Contains(aa.PlayerId))
                    .GroupBy(aa => aa.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        AdminActions = g.OrderByDescending(aa => aa.Created)
                            .Take(10)
                            .ToList()
                    })
                    .ToDictionaryAsync(x => x.PlayerId, x => x.AdminActions);

                foreach (var player in results)
                {
                    if (playerIdToAdminActions.TryGetValue(player.PlayerId, out var adminActions))
                    {
                        player.AdminActions = adminActions;
                    }
                    else
                    {
                        player.AdminActions = new List<AdminAction>();
                    }
                }
            }
        }
        else
        {
            results = await query.ToListAsync();
        }

        // Map to DTOs
        var entries = results.Select(p => p.ToDto()).ToList();

        // Create the result collection
        var data = new CollectionModel<PlayerDto>(entries);

        return new ApiResponse<CollectionModel<PlayerDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Creates new players in the system.
    /// </summary>
    /// <param name="createPlayerDtos">The list of player data to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response if players were created successfully; otherwise, an error response.</returns>
    [HttpPost("players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePlayers([FromBody] List<CreatePlayerDto> createPlayerDtos, CancellationToken cancellationToken = default)
    {
        if (createPlayerDtos == null || !createPlayerDtos.Any())
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                .ToBadRequestResult()
                .ToHttpResult();

        var response = await ((IPlayersApi)this).CreatePlayers(createPlayerDtos);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates a new player in the system.
    /// </summary>
    /// <param name="createPlayerDto">The player data to create.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.CreatePlayer(CreatePlayerDto createPlayerDto)
    {
        if (await context.Players
            .AsNoTracking()
            .AnyAsync(p => p.GameType == createPlayerDto.GameType.ToGameTypeInt() && p.Guid == createPlayerDto.Guid))
            return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.PlayerConflictMessage)).ToConflictResult();

        var player = createPlayerDto.ToEntity();
        player.FirstSeen = DateTime.UtcNow;
        player.LastSeen = DateTime.UtcNow;

        if (IPAddress.TryParse(createPlayerDto.IpAddress, out var ip))
        {
            player.IpAddress = ip.ToString();

            player.PlayerIpAddresses = new List<PlayerIpAddress>
            {
                new PlayerIpAddress
                {
                    Address = ip.ToString(),
                    Added = DateTime.UtcNow,
                    LastUsed = DateTime.UtcNow,
                    ConfidenceScore = 1
                }
            };
        }

        player.PlayerAliases = new List<PlayerAlias>
        {
            new PlayerAlias
            {
                Name = createPlayerDto.Username.Trim(),
                Added = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow,
                ConfidenceScore = 1
            }
        };

        await context.Players.AddAsync(player);
        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates new players in the system.
    /// </summary>
    /// <param name="createPlayerDtos">The list of player data to create.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.CreatePlayers(List<CreatePlayerDto> createPlayerDtos)
    {
        foreach (var createPlayerDto in createPlayerDtos)
        {
            if (await context.Players
                .AsNoTracking()
                .AnyAsync(p => p.GameType == createPlayerDto.GameType.ToGameTypeInt() && p.Guid == createPlayerDto.Guid))
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.PlayerConflictMessage)).ToConflictResult();

            var player = createPlayerDto.ToEntity();
            player.FirstSeen = DateTime.UtcNow;
            player.LastSeen = DateTime.UtcNow;

            if (IPAddress.TryParse(createPlayerDto.IpAddress, out var ip))
            {
                player.IpAddress = ip.ToString();

                player.PlayerIpAddresses = new List<PlayerIpAddress>
                {
                    new PlayerIpAddress
                    {
                        Address = ip.ToString(),
                        Added = DateTime.UtcNow,
                        LastUsed = DateTime.UtcNow,
                        ConfidenceScore = 1
                    }
                };
            }

            player.PlayerAliases = new List<PlayerAlias>
            {
                new PlayerAlias
                {
                    Name = createPlayerDto.Username.Trim(),
                    Added = DateTime.UtcNow,
                    LastUsed = DateTime.UtcNow,
                    ConfidenceScore = 1
                }
            };

            await context.Players.AddAsync(player);
        }

        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Updates an existing player's information.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player to update.</param>
    /// <param name="editPlayerDto">The updated player data.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response if the player was updated; otherwise, an error response.</returns>
    [HttpPatch("players/{playerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlayer(Guid playerId, [FromBody] EditPlayerDto editPlayerDto, CancellationToken cancellationToken = default)
    {
        if (editPlayerDto == null)
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                .ToBadRequestResult()
                .ToHttpResult();

        if (editPlayerDto.PlayerId != playerId)
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage)).ToBadRequestResult().ToHttpResult();

        var response = await ((IPlayersApi)this).UpdatePlayer(editPlayerDto);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Updates an existing player's information.
    /// </summary>
    /// <param name="editPlayerDto">The updated player data.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.UpdatePlayer(EditPlayerDto editPlayerDto)
    {
        var player = await context.Players
            .Include(p => p.PlayerAliases)
            .Include(p => p.PlayerIpAddresses)
            .FirstOrDefaultAsync(p => p.PlayerId == editPlayerDto.PlayerId);

        if (player == null)
            return new ApiResult(HttpStatusCode.NotFound);

        player.Username = editPlayerDto.Username;
        player.IpAddress = editPlayerDto.IpAddress ?? null;
        player.LastSeen = DateTime.UtcNow;

        var playerAlias = player.PlayerAliases.FirstOrDefault(a => a.Name == editPlayerDto.Username);
        if (playerAlias != null)
        {
            playerAlias.LastUsed = DateTime.UtcNow;
            playerAlias.ConfidenceScore++;
        }
        else
        {
            player.PlayerAliases.Add(new PlayerAlias
            {
                Name = editPlayerDto.Username,
                Added = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow,
                ConfidenceScore = 1
            });
        }

        var playerIpAddress = player.PlayerIpAddresses.FirstOrDefault(a => a.Address == editPlayerDto.IpAddress);
        if (playerIpAddress != null)
        {
            playerIpAddress.LastUsed = DateTime.UtcNow;
            playerIpAddress.ConfidenceScore++;
        }
        else
        {
            player.PlayerIpAddresses.Add(new PlayerIpAddress
            {
                Address = editPlayerDto.IpAddress,
                Added = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow,
                ConfidenceScore = 1
            });
        }

        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult();
    }
    private IQueryable<Player> ApplyFilter(IQueryable<Player> query, GameType? gameType, PlayersFilter? filter, string? filterString)
    {
        if (gameType.HasValue)
            query = query.Where(p => p.GameType == gameType.Value.ToGameTypeInt());

        if (filter.HasValue && !string.IsNullOrWhiteSpace(filterString))
        {
            var trimmedFilter = filterString.Trim();

            query = filter.Value switch
            {
                PlayersFilter.UsernameAndGuid => ApplyUsernameAndGuidFilter(query, trimmedFilter),
                PlayersFilter.IpAddress => ApplyIpAddressFilter(query, trimmedFilter),
                _ => query
            };
        }

        return query;
    }

    private IQueryable<Player> ApplyUsernameAndGuidFilter(IQueryable<Player> query, string trimmedFilter)
    {
        // Exact GUID fast-path (32 hex chars) - skip full text for precision
        if (trimmedFilter.Length == 32)
            return query.Where(p => p.Guid == trimmedFilter);

        // Minimal length guard: extremely short terms are noisy; still allow but use simple prefix to leverage indexes
        if (trimmedFilter.Length < 3)
        {
            var prefix = trimmedFilter + "%"; // translates to LIKE 'xx%'
            return query.Where(p =>
                (p.Username != null && EF.Functions.Like(p.Username, prefix)) ||
                (p.Guid != null && EF.Functions.Like(p.Guid, prefix)));
        }

        // Full-text search path (Players.Username, Players.Guid, PlayerAlias.Name)
        // Sanitize user input: wrap in quotes for phrase/prefix, escape embedded quotes
        var sanitized = trimmedFilter.Replace("\"", "\"\"");
        var ftSearch = $"\"{sanitized}*\""; // prefix match for term

        // NOTE: Using FromSqlInterpolated with CONTAINSTABLE to retrieve PlayerIds by rank then join Players once.
        // Using PK (PlayerId) so key index is valid for full-text index.

        var playersFt = context.Players
            .FromSqlInterpolated($@"SELECT p.*
FROM CONTAINSTABLE(Players, (Username, Guid), {ftSearch}) ft
JOIN Players p ON p.PlayerId = ft.[Key]")
            .AsNoTracking();

        var aliasFt = context.Players
            .FromSqlInterpolated($@"SELECT DISTINCT p.*
FROM CONTAINSTABLE(PlayerAlias, (Name), {ftSearch}) fta
JOIN PlayerAlias a ON a.PlayerAliasId = fta.[Key]
JOIN Players p ON p.PlayerId = a.PlayerId")
            .AsNoTracking();

        // UNION ALL then Distinct at EF level avoids duplicate elimination work inside SQL twice.
        var combined = playersFt.Union(aliasFt);

        return combined;
    }

    private IQueryable<Player> ApplyIpAddressFilter(IQueryable<Player> query, string trimmedFilter)
    {
        if (IPAddress.TryParse(trimmedFilter, out IPAddress? filterIpAddress))
        {
            // Exact match when it's a valid IP address
            var directMatches = query.Where(p => p.IpAddress == trimmedFilter);

            // Get players with this exact IP address in their history
            var playerIdsWithExactIpMatch = context.PlayerIpAddresses
                .Where(ip => ip.Address == trimmedFilter)
                .Select(ip => ip.PlayerId)
                .Distinct();

            // Union with exact matches from the IP history
            return directMatches.Union(context.Players.Where(p => playerIdsWithExactIpMatch.Contains(p.PlayerId)));
        }
        else if (trimmedFilter.Length >= 3)
        {
            // For partial IP searches (e.g., subnet searches like "192.168")
            var directMatches = query.Where(p => p.IpAddress != null && p.IpAddress.Contains(trimmedFilter));

            // Get player IDs with matching IP address patterns in their history
            var playerIdsWithPartialIpMatch = context.PlayerIpAddresses
                .Where(ip => ip.Address != null && ip.Address.Contains(trimmedFilter))
                .Select(ip => ip.PlayerId)
                .Distinct();

            // Union the results
            return directMatches.Union(context.Players.Where(p => playerIdsWithPartialIpMatch.Contains(p.PlayerId)));
        }
        else
        {
            // For very short IP fragments, use a more restrictive search
            return query.Where(p => p.IpAddress != null && p.IpAddress.Contains(trimmedFilter));
        }
    }

    private IQueryable<Player> ApplyOrderAndLimits(IQueryable<Player> query, int skipEntries, int takeEntries, PlayersOrder? order)
    {
        // Apply ordering using switch expression
        var orderedQuery = order switch
        {
            PlayersOrder.UsernameAsc => query.OrderBy(p => p.Username),
            PlayersOrder.UsernameDesc => query.OrderByDescending(p => p.Username),
            PlayersOrder.FirstSeenAsc => query.OrderBy(p => p.FirstSeen),
            PlayersOrder.FirstSeenDesc => query.OrderByDescending(p => p.FirstSeen),
            PlayersOrder.LastSeenAsc => query.OrderBy(p => p.LastSeen),
            PlayersOrder.LastSeenDesc => query.OrderByDescending(p => p.LastSeen),
            PlayersOrder.GameTypeAsc => query.OrderBy(p => p.GameType),
            PlayersOrder.GameTypeDesc => query.OrderByDescending(p => p.GameType),
            _ => query.OrderByDescending(p => p.LastSeen)
        };

        return orderedQuery.Skip(skipEntries).Take(takeEntries);
    }

    #region Protected Names

    /// <summary>
    /// Retrieves a paginated list of protected names.
    /// </summary>
    /// <param name="skipEntries">Number of entries to skip for pagination.</param>
    /// <param name="takeEntries">Number of entries to take for pagination.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A paginated collection of protected names.</returns>
    [HttpGet("players/protected-names")]
    [ProducesResponseType<CollectionModel<ProtectedNameDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProtectedNames(
        [FromQuery] int? skipEntries,
        [FromQuery] int? takeEntries,
        CancellationToken cancellationToken = default)
    {
        var skipValue = skipEntries ?? 0;
        var takeValue = takeEntries ?? 20;

        var response = await ((IPlayersApi)this).GetProtectedNames(skipValue, takeValue);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a paginated list of protected names.
    /// </summary>
    /// <param name="skipEntries">Number of entries to skip for pagination.</param>
    /// <param name="takeEntries">Number of entries to take for pagination.</param>
    /// <returns>An API result containing a paginated collection of protected names.</returns>
    async Task<ApiResult<CollectionModel<ProtectedNameDto>>> IPlayersApi.GetProtectedNames(int skipEntries, int takeEntries)
    {
        var totalCount = await context.ProtectedNames.AsNoTracking().CountAsync();

        var query = context.ProtectedNames
            .AsNoTracking()
            .Include(pn => pn.Player)
            .Include(pn => pn.CreatedByUserProfile)
            .OrderBy(pn => pn.Name)
            .Skip(skipEntries)
            .Take(takeEntries);

        var results = await query.ToListAsync();

        var entries = results.Select(pn => pn.ToProtectedNameDto()).ToList();

        var data = new CollectionModel<ProtectedNameDto>(entries);

        return new ApiResponse<CollectionModel<ProtectedNameDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Retrieves a specific protected name by its unique identifier.
    /// </summary>
    /// <param name="protectedNameId">The unique identifier of the protected name to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The protected name details if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("players/protected-names/{protectedNameId:guid}")]
    [ProducesResponseType<ProtectedNameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProtectedName(Guid protectedNameId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetProtectedName(protectedNameId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a specific protected name by its unique identifier.
    /// </summary>
    /// <param name="protectedNameId">The unique identifier of the protected name to retrieve.</param>
    /// <returns>An API result containing the protected name details if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<ProtectedNameDto>> IPlayersApi.GetProtectedName(Guid protectedNameId)
    {
        var protectedName = await context.ProtectedNames
            .AsNoTracking()
            .Include(pn => pn.Player)
            .Include(pn => pn.CreatedByUserProfile)
            .FirstOrDefaultAsync(pn => pn.ProtectedNameId == protectedNameId);

        if (protectedName == null)
            return new ApiResult<ProtectedNameDto>(HttpStatusCode.NotFound);

        var result = protectedName.ToProtectedNameDto();

        return new ApiResponse<ProtectedNameDto>(result).ToApiResult();
    }

    /// <summary>
    /// Retrieves protected names for a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A collection of protected names for the player if the player exists; otherwise, a 404 Not Found response.</returns>
    [HttpGet("players/{playerId:guid}/protected-names")]
    [ProducesResponseType<CollectionModel<ProtectedNameDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProtectedNamesForPlayer(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetProtectedNamesForPlayer(playerId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves protected names for a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <returns>An API result containing a collection of protected names for the player if the player exists; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<CollectionModel<ProtectedNameDto>>> IPlayersApi.GetProtectedNamesForPlayer(Guid playerId)
    {
        if (!await context.Players.AsNoTracking().AnyAsync(p => p.PlayerId == playerId))
            return new ApiResult<CollectionModel<ProtectedNameDto>>(HttpStatusCode.NotFound);

        var query = context.ProtectedNames
            .AsNoTracking()
            .Include(pn => pn.Player)
            .Include(pn => pn.CreatedByUserProfile)
            .Where(pn => pn.PlayerId == playerId);

        var totalCount = await query.CountAsync();

        var results = await query.OrderBy(pn => pn.Name).ToListAsync();

        var entries = results.Select(pn => pn.ToProtectedNameDto()).ToList();

        var data = new CollectionModel<ProtectedNameDto>(entries);

        return new ApiResponse<CollectionModel<ProtectedNameDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, totalCount, 0, totalCount)
        }.ToApiResult();
    }

    /// <summary>
    /// Creates a new protected name.
    /// </summary>
    /// <param name="createProtectedNameDto">The protected name data to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response if the protected name was created; otherwise, appropriate error response.</returns>
    [HttpPost("players/protected-names")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProtectedName([FromBody] CreateProtectedNameDto createProtectedNameDto, CancellationToken cancellationToken = default)
    {
        if (createProtectedNameDto == null)
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                .ToBadRequestResult()
                .ToHttpResult();

        var response = await ((IPlayersApi)this).CreateProtectedName(createProtectedNameDto);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates a new protected name.
    /// </summary>
    /// <param name="createProtectedNameDto">The protected name data to create.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.CreateProtectedName(CreateProtectedNameDto createProtectedNameDto)
    {
        // Check if player exists
        if (!await context.Players.AsNoTracking().AnyAsync(p => p.PlayerId == createProtectedNameDto.PlayerId))
            return new ApiResult(HttpStatusCode.NotFound);

        // Check if the name is already protected
        if (await context.ProtectedNames.AsNoTracking().AnyAsync(pn => pn.Name != null && pn.Name.ToLower() == createProtectedNameDto.Name.ToLower()))
            return new ApiResult(HttpStatusCode.Conflict, new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.ProtectedNameConflictMessage)));

        var protectedName = new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = createProtectedNameDto.PlayerId,
            Name = createProtectedNameDto.Name,
            CreatedOn = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(createProtectedNameDto.AdminId))
            protectedName.CreatedByUserProfile = await context.UserProfiles.FirstOrDefaultAsync(u => u.XtremeIdiotsForumId == createProtectedNameDto.AdminId);

        await context.ProtectedNames.AddAsync(protectedName);
        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Deletes a protected name.
    /// </summary>
    /// <param name="protectedNameId">The unique identifier of the protected name to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, a 404 Not Found response.</returns>
    [HttpDelete("players/protected-names/{protectedNameId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProtectedName(Guid protectedNameId, CancellationToken cancellationToken = default)
    {
        var deleteProtectedNameDto = new DeleteProtectedNameDto(protectedNameId);
        var response = await ((IPlayersApi)this).DeleteProtectedName(deleteProtectedNameDto);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Deletes a protected name.
    /// </summary>
    /// <param name="deleteProtectedNameDto">The protected name deletion data.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto)
    {
        var protectedName = await context.ProtectedNames
            .FirstOrDefaultAsync(pn => pn.ProtectedNameId == deleteProtectedNameDto.ProtectedNameId);

        if (protectedName == null)
            return new ApiResult(HttpStatusCode.NotFound);

        context.ProtectedNames.Remove(protectedName);
        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Retrieves a usage report for a protected name showing all players who have used it.
    /// </summary>
    /// <param name="protectedNameId">The unique identifier of the protected name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A usage report containing details about players who have used the protected name.</returns>
    [HttpGet("players/protected-names/{protectedNameId:guid}/usage-report")]
    [ProducesResponseType<ProtectedNameUsageReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProtectedNameUsageReport(Guid protectedNameId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetProtectedNameUsageReport(protectedNameId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a usage report for a protected name showing all players who have used it.
    /// </summary>
    /// <param name="protectedNameId">The unique identifier of the protected name.</param>
    /// <returns>An API result containing the usage report if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<ProtectedNameUsageReportDto>> IPlayersApi.GetProtectedNameUsageReport(Guid protectedNameId)
    {
        var protectedName = await context.ProtectedNames
            .AsNoTracking()
            .Include(pn => pn.Player)
            .Include(pn => pn.CreatedByUserProfile)
            .FirstOrDefaultAsync(pn => pn.ProtectedNameId == protectedNameId);

        if (protectedName == null)
            return new ApiResult<ProtectedNameUsageReportDto>(HttpStatusCode.NotFound);

        var owningPlayer = await context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == protectedName.PlayerId);

        if (owningPlayer == null)
            return new ApiResult<ProtectedNameUsageReportDto>(HttpStatusCode.NotFound);

        // Find all player aliases that match this protected name
        var matchingAliases = await context.PlayerAliases
            .AsNoTracking()
            .Include(pa => pa.Player)
            .Where(pa => pa.Name != null && protectedName.Name != null && pa.Name.ToLower() == protectedName.Name.ToLower())
            .OrderByDescending(pa => pa.LastUsed)
            .ToListAsync();

        var usageInstances = new List<ProtectedNameUsageReportDto.PlayerUsageDto>();

        // Group by player and create usage instances
        foreach (var group in matchingAliases.GroupBy(a => a.PlayerId))
        {
            var player = group.First().Player;
            if (player != null)
            {
                var isOwner = player.PlayerId == protectedName.PlayerId;

                usageInstances.Add(new ProtectedNameUsageReportDto.PlayerUsageDto
                {
                    PlayerId = player.PlayerId,
                    Username = player.Username ?? string.Empty,
                    IsOwner = isOwner,
                    LastUsed = group.Max(a => a.LastUsed),
                    UsageCount = group.Count()
                });
            }
        }

        var result = new ProtectedNameUsageReportDto
        {
            ProtectedName = protectedName.ToProtectedNameDto(),
            OwningPlayer = owningPlayer.ToDto(),
            UsageInstances = usageInstances
        };

        return new ApiResponse<ProtectedNameUsageReportDto>(result).ToApiResult();
    }

    #endregion
    #region Player Tags    
    /// <summary>
    /// Retrieves all tags associated with a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A collection of player tags if the player exists; otherwise, a 404 Not Found response.</returns>
    [HttpGet("players/{playerId:guid}/tags")]
    [ProducesResponseType<CollectionModel<PlayerTagDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerTags(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayerTags(playerId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves all tags associated with a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <returns>An API result containing a collection of player tags if the player exists; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<CollectionModel<PlayerTagDto>>> IPlayersApi.GetPlayerTags(Guid playerId)
    {
        // Check if the player exists
        if (!await context.Players.AsNoTracking().AnyAsync(p => p.PlayerId == playerId))
            return new ApiResult<CollectionModel<PlayerTagDto>>(HttpStatusCode.NotFound);

        var playerTags = await context.PlayerTags
            .AsNoTracking()
            .Include(pt => pt.Tag)
            .Include(pt => pt.UserProfile)
            .Where(pt => pt.PlayerId == playerId)
            .ToListAsync();

        var entries = playerTags.Select(pt => pt.ToDto()).ToList();

        var result = new CollectionModel<PlayerTagDto>(entries);

        return new ApiResponse<CollectionModel<PlayerTagDto>>(result)
        {
            Pagination = new ApiPagination(entries.Count, entries.Count, 0, entries.Count)
        }.ToApiResult();
    }
    /// <summary>
    /// Adds a new tag to a player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="playerTagDto">The player tag details to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, appropriate error response.</returns>
    [HttpPost("players/{playerId:guid}/tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPlayerTag(Guid playerId, [FromBody] PlayerTagDto playerTagDto, CancellationToken cancellationToken = default)
    {
        // Ensure the path playerId and body playerId match
        if (playerTagDto.PlayerId != null && playerTagDto.PlayerId.Value != playerId)
        {
            return new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.PlayerIdMismatchMessage))
                .ToBadRequestResult()
                .ToHttpResult();
        }

        // Set the playerId in the DTO to match the URL
        playerTagDto.PlayerId = playerId;

        var response = await ((IPlayersApi)this).AddPlayerTag(playerId, playerTagDto);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Adds a new tag to a player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="playerTagDto">The player tag details to add.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto)
    {
        if (playerTagDto.TagId == null)
            return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.MissingEntityId, ApiErrorMessages.TagIdRequiredMessage)));

        // Check if the player exists
        if (!await context.Players.AsNoTracking().AnyAsync(p => p.PlayerId == playerId))
            return new ApiResult(HttpStatusCode.NotFound);

        // Check if the tag exists
        var tagExists = await context.Tags.AsNoTracking().AnyAsync(t => t.TagId == playerTagDto.TagId);
        if (!tagExists)
            return new ApiResult(HttpStatusCode.NotFound);

        // Check if player already has this tag
        var exists = await context.PlayerTags.AsNoTracking().AnyAsync(pt => pt.PlayerId == playerId && pt.TagId == playerTagDto.TagId);
        if (exists)
            return new ApiResult(HttpStatusCode.Conflict, new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.PlayerTagConflictMessage)));

        var playerTag = playerTagDto.ToEntity();
        playerTag.PlayerId = playerId;
        playerTag.Assigned = DateTime.UtcNow;

        context.PlayerTags.Add(playerTag);
        await context.SaveChangesAsync();
        return new ApiResponse().ToApiResult();
    }
    /// <summary>
    /// Removes a tag from a player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="playerTagId">The unique identifier of the player tag to remove.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, appropriate error response.</returns>
    [HttpDelete("players/{playerId:guid}/tags/{playerTagId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePlayerTag(Guid playerId, Guid playerTagId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).RemovePlayerTag(playerId, playerTagId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Removes a tag from a player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="playerTagId">The unique identifier of the player tag to remove.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.RemovePlayerTag(Guid playerId, Guid playerTagId)
    {
        // Check if the player exists
        if (!await context.Players.AsNoTracking().AnyAsync(p => p.PlayerId == playerId))
            return new ApiResult(HttpStatusCode.NotFound);

        var playerTag = await context.PlayerTags.FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId && pt.PlayerId == playerId);

        if (playerTag == null)
            return new ApiResult(HttpStatusCode.NotFound);

        context.PlayerTags.Remove(playerTag);
        await context.SaveChangesAsync();
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Retrieves a player tag by its unique identifier.
    /// </summary>
    /// <param name="playerTagId">The unique identifier of the player tag.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The player tag details if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("players/tags/{playerTagId:guid}")]
    [ProducesResponseType<PlayerTagDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerTagById(Guid playerTagId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayerTagById(playerTagId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a player tag by its unique identifier.
    /// </summary>
    /// <param name="playerTagId">The unique identifier of the player tag.</param>
    /// <returns>An API result containing the player tag details if found; otherwise, a 404 Not Found response.</returns>
    async Task<ApiResult<PlayerTagDto>> IPlayersApi.GetPlayerTagById(Guid playerTagId)
    {
        var playerTag = await context.PlayerTags
            .AsNoTracking()
            .Include(pt => pt.Player)
            .Include(pt => pt.Tag)
            .Include(pt => pt.UserProfile)
            .FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId);

        if (playerTag == null)
            return new ApiResult<PlayerTagDto>(HttpStatusCode.NotFound);

        var result = playerTag.ToDto();
        return new ApiResponse<PlayerTagDto>(result).ToApiResult();
    }
    /// <summary>
    /// Removes a player tag by its unique identifier.
    /// </summary>
    /// <param name="playerTagId">The unique identifier of the player tag to remove.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, appropriate error response.</returns>
    [HttpDelete("players/tags/{playerTagId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePlayerTagById(Guid playerTagId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).RemovePlayerTagById(playerTagId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Removes a player tag by its unique identifier.
    /// </summary>
    /// <param name="playerTagId">The unique identifier of the player tag to remove.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.RemovePlayerTagById(Guid playerTagId)
    {
        var playerTag = await context.PlayerTags
            .FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId);

        if (playerTag == null)
            return new ApiResult(HttpStatusCode.NotFound);

        context.PlayerTags.Remove(playerTag);
        await context.SaveChangesAsync();
        return new ApiResponse().ToApiResult();
    }

    #endregion

    #region Players with IP Address
    /// <summary>
    /// Retrieves all players associated with a specific IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to search for.</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <param name="order">The order to sort the results by.</param>
    /// <param name="playerEntityOptions">Options for including related player data.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A collection of players associated with the IP address.</returns>
    [HttpGet("players/with-ip-address/{ipAddress}")]
    [ProducesResponseType<CollectionModel<PlayerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlayersWithIpAddress(
        string ipAddress,
        [FromQuery] int skipEntries,
        [FromQuery] int takeEntries,
        [FromQuery] PlayersOrder? order,
        [FromQuery] PlayerEntityOptions playerEntityOptions,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayersWithIpAddress(ipAddress, skipEntries, takeEntries, order, playerEntityOptions);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves all players associated with a specific IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to search for.</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <param name="order">The order to sort the results by.</param>
    /// <param name="playerEntityOptions">Options for including related player data.</param>
    /// <returns>An API result containing a collection of players associated with the IP address.</returns>
    async Task<ApiResult<CollectionModel<PlayerDto>>> IPlayersApi.GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return new ApiResult<CollectionModel<PlayerDto>>(HttpStatusCode.BadRequest, new ApiResponse<CollectionModel<PlayerDto>>(null, new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage)));

        // Filter players related to this IP address 
        var query = context.Players
            .AsNoTracking()
            .Include(p => p.PlayerIpAddresses.Where(i => i.Address == ipAddress))
            .Include(p => p.PlayerTags).ThenInclude(pt => pt.Tag)
            .Where(p => p.PlayerIpAddresses.Any(i => i.Address == ipAddress));

        // Filter out test server guids
        query = query.Where(p => p.Guid != null && (!p.Guid.Contains("test") && !p.Guid.Contains("server")));

        // Get total count before ordering and pagination
        var totalCount = await query.CountAsync();

        // Apply ordering using switch expression
        query = order switch
        {
            PlayersOrder.UsernameAsc => query.OrderBy(p => p.Username),
            PlayersOrder.UsernameDesc => query.OrderByDescending(p => p.Username),
            PlayersOrder.LastSeenAsc => query.OrderBy(p => p.LastSeen),
            PlayersOrder.LastSeenDesc => query.OrderByDescending(p => p.LastSeen),
            PlayersOrder.FirstSeenAsc => query.OrderBy(p => p.FirstSeen),
            PlayersOrder.FirstSeenDesc => query.OrderByDescending(p => p.FirstSeen),
            PlayersOrder.GameTypeAsc => query.OrderBy(p => p.GameType),
            PlayersOrder.GameTypeDesc => query.OrderByDescending(p => p.GameType),
            _ => query.OrderByDescending(p => p.LastSeen) // Default ordering
        };

        // Apply pagination
        query = query.Skip(skipEntries).Take(takeEntries);

        // Execute the final query
        var players = await query.ToListAsync();

        // Include related data based on options
        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            players.ForEach(p => context.Entry(p).Collection(p => p.PlayerAliases).Load());

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
            players.ForEach(p => context.Entry(p).Collection(p => p.PlayerIpAddresses).Load());

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
            players.ForEach(p => context.Entry(p).Collection(p => p.AdminActions).Load());

        // Map to DTOs
        var playerDtos = players.Select(p => p.ToDto()).ToList();

        // Create result
        var data = new CollectionModel<PlayerDto>(playerDtos);

        return new ApiResponse<CollectionModel<PlayerDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
        }.ToApiResult();
    }
    #endregion

    #region Player IP Addresses

    /// <summary>
    /// Retrieves all IP addresses associated with a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <param name="order">The order to sort the results by.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A collection of IP addresses associated with the player.</returns>
    [HttpGet("players/{playerId:guid}/ip-addresses")]
    [ProducesResponseType<CollectionModel<IpAddressDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerIpAddresses(
        Guid playerId,
        [FromQuery] int skipEntries,
        [FromQuery] int takeEntries,
        [FromQuery] IpAddressesOrder? order,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayerIpAddresses(playerId, skipEntries, takeEntries, order);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves all IP addresses associated with a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <param name="order">The order to sort the results by.</param>
    /// <returns>An API result containing a collection of IP addresses associated with the player.</returns>
    async Task<ApiResult<CollectionModel<IpAddressDto>>> IPlayersApi.GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order)
    {
        var player = await context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);

        if (player == null)
            return new ApiResult<CollectionModel<IpAddressDto>>(HttpStatusCode.NotFound);

        var query = context.PlayerIpAddresses
            .AsNoTracking()
            .Where(pip => pip.PlayerId == playerId)
            .AsQueryable();

        // Apply ordering using switch expression
        query = order switch
        {
            IpAddressesOrder.AddressAsc => query.OrderBy(pip => pip.Address),
            IpAddressesOrder.AddressDesc => query.OrderByDescending(pip => pip.Address),
            IpAddressesOrder.AddedAsc => query.OrderBy(pip => pip.Added),
            IpAddressesOrder.AddedDesc => query.OrderByDescending(pip => pip.Added),
            IpAddressesOrder.LastUsedAsc => query.OrderBy(pip => pip.LastUsed),
            IpAddressesOrder.LastUsedDesc => query.OrderByDescending(pip => pip.LastUsed),
            IpAddressesOrder.ConfidenceScoreAsc => query.OrderBy(pip => pip.ConfidenceScore),
            IpAddressesOrder.ConfidenceScoreDesc => query.OrderByDescending(pip => pip.ConfidenceScore),
            _ => query.OrderByDescending(pip => pip.LastUsed) // Default ordering
        };

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync();

        // Map to DTOs
        var dtos = items.Select(item => item.ToIpAddressDto()).ToList();

        // Create and return the response
        var data = new CollectionModel<IpAddressDto>(dtos);

        return new ApiResponse<CollectionModel<IpAddressDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    #endregion

    #region Player Aliases APIs

    /// <summary>
    /// Retrieves all aliases associated with a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A collection of aliases associated with the player.</returns>
    [HttpGet("players/{playerId:guid}/aliases")]
    [ProducesResponseType<CollectionModel<PlayerAliasDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerAliases(
        Guid playerId,
        [FromQuery] int skipEntries,
        [FromQuery] int takeEntries,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).GetPlayerAliases(playerId, skipEntries, takeEntries);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves all aliases associated with a specific player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <returns>An API result containing a collection of aliases associated with the player.</returns>
    async Task<ApiResult<CollectionModel<PlayerAliasDto>>> IPlayersApi.GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries)
    {
        // Check if the player exists - using AnyAsync instead of FirstOrDefaultAsync for better performance
        var playerExists = await context.Players
            .AsNoTracking()
            .AnyAsync(p => p.PlayerId == playerId);

        if (!playerExists)
            return new ApiResult<CollectionModel<PlayerAliasDto>>(HttpStatusCode.NotFound);

        // Build the base query for aliases
        var baseQuery = context.PlayerAliases
            .AsNoTracking()
            .Where(pa => pa.PlayerId == playerId);

        // Calculate total count before applying pagination
        var totalCount = await baseQuery.CountAsync();

        // Apply ordering and pagination
        var aliases = await baseQuery
            .OrderByDescending(pa => pa.LastUsed)
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync();

        // Map the aliases to DTOs
        var aliasesDto = aliases.Select(a => a.ToPlayerAliasDto()).ToList();

        var data = new CollectionModel<PlayerAliasDto>(aliasesDto);

        return new ApiResponse<CollectionModel<PlayerAliasDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Adds a new alias to a player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="createPlayerAliasDto">The alias details to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, a 404 Not Found response.</returns>
    [HttpPost("players/{playerId:guid}/aliases")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPlayerAlias(Guid playerId, [FromBody] CreatePlayerAliasDto createPlayerAliasDto, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).AddPlayerAlias(playerId, createPlayerAliasDto);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Adds a new alias to a player.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="createPlayerAliasDto">The alias details to add.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto)
    {
        // Check if the player exists
        var player = await context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null)
            return new ApiResult(HttpStatusCode.NotFound);

        // Check if the alias already exists
        var existingAlias = await context.PlayerAliases
            .FirstOrDefaultAsync(pa => pa.PlayerId == playerId && pa.Name == createPlayerAliasDto.Name);

        if (existingAlias != null)
        {
            // If alias exists, just update the LastUsed and increment the confidence score
            existingAlias.LastUsed = DateTime.UtcNow;
            existingAlias.ConfidenceScore++;
        }
        else
        {
            // Create a new alias
            var newAlias = new PlayerAlias
            {
                PlayerId = playerId,
                Name = createPlayerAliasDto.Name,
                Added = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow,
                ConfidenceScore = 1
            };

            await context.PlayerAliases.AddAsync(newAlias);
        }

        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Updates an existing player alias.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="aliasId">The unique identifier of the alias to update.</param>
    /// <param name="updatePlayerAliasDto">The updated alias details.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, a 404 Not Found response.</returns>
    [HttpPatch("players/{playerId:guid}/aliases/{aliasId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlayerAlias(Guid playerId, Guid aliasId, [FromBody] CreatePlayerAliasDto updatePlayerAliasDto, CancellationToken cancellationToken = default)
    {
        if (updatePlayerAliasDto == null || string.IsNullOrWhiteSpace(updatePlayerAliasDto.Name))
            return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage)).ToBadRequestResult().ToHttpResult();
        var response = await ((IPlayersApi)this).UpdatePlayerAlias(playerId, aliasId, updatePlayerAliasDto);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Updates an existing player alias.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="aliasId">The unique identifier of the alias to update.</param>
    /// <param name="updatePlayerAliasDto">The updated alias details.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto)
    {
        // Check if the alias exists and belongs to the player
        var alias = await context.PlayerAliases
            .FirstOrDefaultAsync(pa => pa.PlayerAliasId == aliasId && pa.PlayerId == playerId);

        if (alias == null)
            return new ApiResult(HttpStatusCode.NotFound);

        // Partial update: only update name if different
        if (!string.IsNullOrWhiteSpace(updatePlayerAliasDto.Name) && alias.Name != updatePlayerAliasDto.Name)
            alias.Name = updatePlayerAliasDto.Name.Trim();
        alias.LastUsed = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Deletes a player alias.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="aliasId">The unique identifier of the alias to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response if successful; otherwise, a 404 Not Found response.</returns>
    [HttpDelete("players/{playerId:guid}/aliases/{aliasId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlayerAlias(Guid playerId, Guid aliasId, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).DeletePlayerAlias(playerId, aliasId);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Deletes a player alias.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <param name="aliasId">The unique identifier of the alias to delete.</param>
    /// <returns>An API result indicating success or failure of the operation.</returns>
    async Task<ApiResult> IPlayersApi.DeletePlayerAlias(Guid playerId, Guid aliasId)
    {
        // Check if the alias exists and belongs to the player
        var alias = await context.PlayerAliases
            .FirstOrDefaultAsync(pa => pa.PlayerAliasId == aliasId && pa.PlayerId == playerId);

        if (alias == null)
            return new ApiResult(HttpStatusCode.NotFound);

        // Remove the alias
        context.PlayerAliases.Remove(alias);
        await context.SaveChangesAsync();

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Searches for player aliases by name.
    /// </summary>
    /// <param name="aliasSearch">The search term to match against alias names (minimum 3 characters).</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A collection of aliases matching the search criteria.</returns>
    [HttpGet("aliases/search")]
    [ProducesResponseType<CollectionModel<PlayerAliasDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPlayersByAlias([FromQuery] string aliasSearch, [FromQuery] int skipEntries, [FromQuery] int takeEntries, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayersApi)this).SearchPlayersByAlias(aliasSearch, skipEntries, takeEntries);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Searches for player aliases by name.
    /// </summary>
    /// <param name="aliasSearch">The search term to match against alias names (minimum 3 characters).</param>
    /// <param name="skipEntries">The number of entries to skip for pagination.</param>
    /// <param name="takeEntries">The number of entries to take for pagination.</param>
    /// <returns>An API result containing a collection of aliases matching the search criteria.</returns>
    async Task<ApiResult<CollectionModel<PlayerAliasDto>>> IPlayersApi.SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries)
    {
        if (string.IsNullOrWhiteSpace(aliasSearch) || aliasSearch.Length < 3)
            return new ApiResult<CollectionModel<PlayerAliasDto>>(HttpStatusCode.BadRequest, new ApiResponse<CollectionModel<PlayerAliasDto>>(null, new ApiError(ApiErrorCodes.InvalidRequest, ApiErrorMessages.InvalidRequestBodyMessage)));

        var trimmedSearch = aliasSearch.Trim();

        // Find aliases that match the search term
        var query = context.PlayerAliases
            .AsNoTracking()
            .Where(pa => pa.Name != null && pa.Name.Contains(trimmedSearch))
            .OrderByDescending(pa => pa.LastUsed);

        var totalCount = await query.CountAsync();

        var aliases = await query
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync();

        // Map the aliases to DTOs
        var aliasesDto = aliases.Select(a => a.ToPlayerAliasDto()).ToList();

        var data = new CollectionModel<PlayerAliasDto>(aliasesDto);

        return new ApiResponse<CollectionModel<PlayerAliasDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
        }.ToApiResult();
    }
}

#endregion
