using System.Net;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags;
using XtremeIdiots.Portal.RepositoryWebApi.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
public class PlayersController : ControllerBase, IPlayersApi
{
    private readonly PortalDbContext context;
    private readonly IMapper mapper;
    private readonly IMemoryCache _memoryCache;

    public PlayersController(
        PortalDbContext context,
        IMapper mapper,
        IMemoryCache memoryCache)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this._memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    [HttpGet]
    [Route("players/{playerId}")]
    public async Task<IActionResult> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
    {
        var response = await ((IPlayersApi)this).GetPlayer(playerId, playerEntityOptions);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<PlayerDto>> IPlayersApi.GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
    {
        var player = await context.Players.SingleOrDefaultAsync(p => p.PlayerId == playerId);

        if (player == null)
            return new ApiResponseDto<PlayerDto>(HttpStatusCode.NotFound);

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            player.PlayerAliases = await context.PlayerAliases.OrderByDescending(pa => pa.LastUsed).Where(pa => pa.PlayerId == player.PlayerId).ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
            player.PlayerIpAddresses = await context.PlayerIpAddresses.OrderByDescending(pip => pip.LastUsed).Where(pip => pip.PlayerId == player.PlayerId).ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
            player.AdminActions = await context.AdminActions.OrderByDescending(aa => aa.Created).Where(aa => aa.PlayerId == player.PlayerId).ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.ProtectedNames))
            player.ProtectedNames = await context.ProtectedNames.Include(pn => pn.CreatedByUserProfile).OrderByDescending(pn => pn.CreatedOn).Where(pn => pn.PlayerId == player.PlayerId).ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags))
            player.PlayerTags = await context.PlayerTags.Include(pt => pt.Tag).Where(pt => pt.PlayerId == player.PlayerId).ToListAsync();

        var result = mapper.Map<PlayerDto>(player);

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.RelatedPlayers))
        {
            var playerIpAddresses = await context.PlayerIpAddresses
                .Include(ip => ip.Player)
                .Where(ip => ip.Address == player.IpAddress && ip.PlayerId != player.PlayerId)
                .ToListAsync();

            result.RelatedPlayers = playerIpAddresses.Select(pip => mapper.Map<RelatedPlayerDto>(pip)).ToList();
        }

        // Optionally, map tags to DTO if requested
        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags))
            result.Tags = player.PlayerTags?.Select(mapper.Map<PlayerTagDto>).ToList() ?? new List<PlayerTagDto>();

        return new ApiResponseDto<PlayerDto>(HttpStatusCode.OK, result);
    }

    [HttpHead]
    [Route("players/by-game-type/{gameType}/{guid}")]
    public async Task<IActionResult> HeadPlayerByGameType(GameType gameType, string guid)
    {
        var response = await ((IPlayersApi)this).HeadPlayerByGameType(gameType, guid);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.HeadPlayerByGameType(GameType gameType, string guid)
    {
        var player = await context.Players.AnyAsync(p => p.GameType == gameType.ToGameTypeInt() && p.Guid == guid);

        if (player == false)
            return new ApiResponseDto<PlayerDto>(HttpStatusCode.NotFound);

        return new ApiResponseDto<PlayerDto>(HttpStatusCode.OK);
    }

    [HttpGet]
    [Route("players/by-game-type/{gameType}/{guid}")]
    public async Task<IActionResult> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
    {
        var response = await ((IPlayersApi)this).GetPlayerByGameType(gameType, guid, playerEntityOptions);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<PlayerDto>> IPlayersApi.GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
    {
        var player = await context.Players.SingleOrDefaultAsync(p => p.GameType == gameType.ToGameTypeInt() && p.Guid == guid);

        if (player == null)
            return new ApiResponseDto<PlayerDto>(HttpStatusCode.NotFound);

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            player.PlayerAliases = await context.PlayerAliases.OrderByDescending(pa => pa.LastUsed).Where(pa => pa.PlayerId == player.PlayerId).ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
            player.PlayerIpAddresses = await context.PlayerIpAddresses.OrderByDescending(pip => pip.LastUsed).Where(pip => pip.PlayerId == player.PlayerId).ToListAsync();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
            player.AdminActions = await context.AdminActions.OrderByDescending(aa => aa.Created).Where(aa => aa.PlayerId == player.PlayerId).ToListAsync();

        var result = mapper.Map<PlayerDto>(player);

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.RelatedPlayers))
        {
            var playerIpAddresses = await context.PlayerIpAddresses
                .Include(ip => ip.Player)
                .Where(ip => ip.Address == player.IpAddress && ip.PlayerId != player.PlayerId)
                .ToListAsync();

            result.RelatedPlayers = playerIpAddresses.Select(pip => mapper.Map<RelatedPlayerDto>(pip)).ToList();
        }

        // Optionally, map tags to DTO if requested
        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Tags))
            result.Tags = player.PlayerTags?.Select(mapper.Map<PlayerTagDto>).ToList() ?? new List<PlayerTagDto>();

        return new ApiResponseDto<PlayerDto>(HttpStatusCode.OK, result);
    }

    [HttpGet]
    [Route("players")]
    public async Task<IActionResult> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int? skipEntries, int? takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        if (!skipEntries.HasValue)
            skipEntries = 0;

        if (!takeEntries.HasValue)
            takeEntries = 20;

        var response = await ((IPlayersApi)this).GetPlayers(gameType, filter, filterString, skipEntries.Value, takeEntries.Value, order, playerEntityOptions);

        return response.ToHttpResult();
    }
    async Task<ApiResponseDto<PlayersCollectionDto>> IPlayersApi.GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        // Only count the total records once every 10 minutes by using a cached counter
        // This approach avoids counting the entire table for every search
        var cacheKey = $"PlayerCount_{gameType}";
        int totalCount;

        if (!_memoryCache.TryGetValue(cacheKey, out totalCount))
        {
            var countQuery = context.Players.AsQueryable();
            if (gameType.HasValue)
                countQuery = countQuery.Where(p => p.GameType == gameType.Value.ToGameTypeInt());

            totalCount = await countQuery.CountAsync();

            // Cache the count for 10 minutes
            _memoryCache.Set(cacheKey, totalCount, TimeSpan.FromMinutes(10));
        }

        // Start building the query for filtered results
        var query = context.Players.AsQueryable();

        // Apply the filter for the specific search
        query = ApplyFilter(query, gameType, filter, filterString);

        // For filtered results, always count but check cache first for common queries
        int filteredCount;
        var filteredCacheKey = $"FilteredPlayerCount_{gameType}_{filter}_{filterString?.GetHashCode()}";

        if (string.IsNullOrWhiteSpace(filterString) && !_memoryCache.TryGetValue(filteredCacheKey, out filteredCount))
        {
            // Only cache counts if the filter string is empty (meaning this is likely a common query)
            filteredCount = await query.CountAsync();

            // Cache the filtered count for 5 minutes (shorter than total count)
            _memoryCache.Set(filteredCacheKey, filteredCount, TimeSpan.FromMinutes(5));
        }
        else
        {
            // For all other cases, just count without caching
            filteredCount = await query.CountAsync();
        }

        // Apply ordering and pagination before fetching results
        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);

        // Create a list to hold our results
        List<Player> results;

        // Optimize the query based on what related data is needed
        if (playerEntityOptions != PlayerEntityOptions.None)
        {
            // Track what's been loaded with each query to avoid redundant queries
            var loadedRelatedData = new Dictionary<string, bool>();

            // Construct a single optimized query that includes only what's needed
            if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
            {
                // For the main query, load player data with top 10 aliases
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

                loadedRelatedData["Aliases"] = true;
            }
            else
            {
                // If not loading aliases, just get the player data
                results = await query.ToListAsync();
            }
            // Extract player IDs for use in subsequent queries
            var playerIds = results.Select(p => p.PlayerId).ToList();

            // Use batch loading for related data when needed
            // Load IP addresses if requested (optimized query)
            if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses) && !loadedRelatedData.ContainsKey("IpAddresses"))
            {
                // Get most recent 10 IP addresses per player in a single query                
                var playerIdToIpAddresses = await context.PlayerIpAddresses
                    .Where(ip => ip.PlayerId != null && playerIds.Contains(ip.PlayerId.Value))
                    .GroupBy(ip => ip.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key!.Value, // Using null-forgiving operator since we've filtered out nulls
                        IpAddresses = g.OrderByDescending(ip => ip.LastUsed)
                            .Take(10)
                            .ToList()
                    })
                    .ToDictionaryAsync(x => x.PlayerId, x => x.IpAddresses);

                // Efficiently assign IP addresses to players
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

            // Load admin actions if requested (optimized query)
            if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions) && !loadedRelatedData.ContainsKey("AdminActions"))
            {
                // Get most recent 10 admin actions per player in a single query
                var playerIdToAdminActions = await context.AdminActions
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

                // Efficiently assign admin actions to players
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
            // If no related data is requested, just get the player data
            results = await query.ToListAsync();
        }

        // Map to DTOs
        var entries = results.Select(p => mapper.Map<PlayerDto>(p)).ToList();

        // Create the result collection
        var result = new PlayersCollectionDto
        {
            TotalRecords = totalCount,
            FilteredRecords = filteredCount,
            Entries = entries
        };

        return new ApiResponseDto<PlayersCollectionDto>(HttpStatusCode.OK, result);
    }

    [HttpPost]
    [Route("players")]
    public async Task<IActionResult> CreatePlayers()
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        List<CreatePlayerDto>? createPlayerDtos;
        try
        {
            createPlayerDtos = JsonConvert.DeserializeObject<List<CreatePlayerDto>>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (createPlayerDtos == null || !createPlayerDtos.Any())
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null or did not contain any entries" }).ToHttpResult();

        var response = await ((IPlayersApi)this).CreatePlayers(createPlayerDtos);

        return response.ToHttpResult();
    }

    Task<ApiResponseDto> IPlayersApi.CreatePlayer(CreatePlayerDto createPlayerDto)
    {
        throw new NotImplementedException();
    }

    async Task<ApiResponseDto> IPlayersApi.CreatePlayers(List<CreatePlayerDto> createPlayerDtos)
    {
        foreach (var createPlayerDto in createPlayerDtos)
        {
            if (await context.Players.AnyAsync(p => p.GameType == createPlayerDto.GameType.ToGameTypeInt() && p.Guid == createPlayerDto.Guid))
                return new ApiResponseDto(HttpStatusCode.Conflict, new List<string> { $"Player with gameType '{createPlayerDto.GameType}' and guid '{createPlayerDto.Guid}' already exists" });

            var player = mapper.Map<Player>(createPlayerDto);
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

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpPatch]
    [Route("players/{playerId}")]
    public async Task<IActionResult> UpdatePlayer(Guid playerId)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        EditPlayerDto? editPlayerDto;
        try
        {
            editPlayerDto = JsonConvert.DeserializeObject<EditPlayerDto>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (editPlayerDto == null)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

        if (editPlayerDto.PlayerId != playerId)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request entity identifiers did not match" }).ToHttpResult();

        var response = await ((IPlayersApi)this).UpdatePlayer(editPlayerDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.UpdatePlayer(EditPlayerDto editPlayerDto)
    {
        var player = await context.Players
                .Include(p => p.PlayerAliases)
                .Include(p => p.PlayerIpAddresses)
                .SingleOrDefaultAsync(p => p.PlayerId == editPlayerDto.PlayerId);

        if (player == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

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

        return new ApiResponseDto(HttpStatusCode.OK);
    }
    private IQueryable<Player> ApplyFilter(IQueryable<Player> query, GameType? gameType, PlayersFilter? filter, string? filterString)
    {
        if (gameType.HasValue)
            query = query.Where(p => p.GameType == gameType.Value.ToGameTypeInt());

        if (filter.HasValue && !string.IsNullOrWhiteSpace(filterString))
        {
            var trimmedFilter = filterString.Trim();

            switch (filter)
            {
                case PlayersFilter.UsernameAndGuid:
                    if (filterString.Length == 32) // Search is for a player GUID
                    {
                        // Exact match on GUID is most efficient
                        query = query.Where(p => p.Guid == filterString);
                    }
                    else if (trimmedFilter.Length >= 3)
                    {
                        // For search strings with at least 3 characters, use a more optimized approach
                        // First, get players matching username or GUID directly (leveraging indexes)
                        var directMatches = query.Where(p => (p.Username != null && p.Username.Contains(trimmedFilter)) ||
                                                          (p.Guid != null && p.Guid.Contains(trimmedFilter)));

                        // Then, get player IDs with matching aliases (as a separate efficient query)
                        var playerIdsWithMatchingAliases = context.PlayerAliases
                            .Where(a => a.Name != null && a.Name.Contains(trimmedFilter))
                            .Select(a => a.PlayerId)
                            .Distinct();

                        // Finally, get players matching those IDs
                        var aliasMatches = query.Where(p => playerIdsWithMatchingAliases.Contains(p.PlayerId));

                        // Union the results (removes duplicates)
                        query = directMatches.Union(aliasMatches);
                    }
                    else
                    {
                        // For very short search strings, fall back to the original approach but without alias search
                        // to avoid excessive matching and poor performance
                        query = query.Where(p => (p.Username != null && p.Username.Contains(trimmedFilter)) ||
                                              (p.Guid != null && p.Guid.Contains(trimmedFilter)));
                    }
                    break;

                case PlayersFilter.IpAddress:
                    if (IPAddress.TryParse(trimmedFilter, out IPAddress? filterIpAddress))
                    {
                        // Exact match when it's a valid IP address
                        query = query.Where(p => p.IpAddress == trimmedFilter);

                        // Get players with this exact IP address in their history
                        var playerIdsWithExactIpMatch = context.PlayerIpAddresses
                            .Where(ip => ip.Address == trimmedFilter)
                            .Select(ip => ip.PlayerId)
                            .Distinct();

                        // Union with exact matches from the IP history
                        query = query.Union(context.Players.Where(p => playerIdsWithExactIpMatch.Contains(p.PlayerId)));
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
                        query = directMatches.Union(context.Players.Where(p => playerIdsWithPartialIpMatch.Contains(p.PlayerId)));
                    }
                    else
                    {
                        // For very short IP fragments, use a more restrictive search
                        query = query.Where(p => p.IpAddress != null && p.IpAddress.Contains(trimmedFilter));
                    }
                    break;
            }
        }

        return query;
    }

    private IQueryable<Player> ApplyOrderAndLimits(IQueryable<Player> query, int skipEntries, int takeEntries, PlayersOrder? order)
    {
        switch (order)
        {
            case PlayersOrder.UsernameAsc:
                query = query.OrderBy(p => p.Username).AsQueryable();
                break;
            case PlayersOrder.UsernameDesc:
                query = query.OrderByDescending(p => p.Username).AsQueryable();
                break;
            case PlayersOrder.FirstSeenAsc:
                query = query.OrderBy(p => p.FirstSeen).AsQueryable();
                break;
            case PlayersOrder.FirstSeenDesc:
                query = query.OrderByDescending(p => p.FirstSeen).AsQueryable();
                break;
            case PlayersOrder.LastSeenAsc:
                query = query.OrderBy(p => p.LastSeen).AsQueryable();
                break;
            case PlayersOrder.LastSeenDesc:
                query = query.OrderByDescending(p => p.LastSeen).AsQueryable();
                break;
            case PlayersOrder.GameTypeAsc:
                query = query.OrderBy(p => p.GameType).AsQueryable();
                break;
            case PlayersOrder.GameTypeDesc:
                query = query.OrderByDescending(p => p.GameType).AsQueryable();
                break;
        }

        query = query.Skip(skipEntries).AsQueryable();
        query = query.Take(takeEntries).AsQueryable();

        return query;
    }

    #region Protected Names

    [HttpGet]
    [Route("players/protected-names")]
    public async Task<IActionResult> GetProtectedNames(int? skipEntries, int? takeEntries)
    {
        if (!skipEntries.HasValue)
            skipEntries = 0;

        if (!takeEntries.HasValue)
            takeEntries = 20;

        var response = await ((IPlayersApi)this).GetProtectedNames(skipEntries.Value, takeEntries.Value);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<ProtectedNamesCollectionDto>> IPlayersApi.GetProtectedNames(int skipEntries, int takeEntries)
    {
        var query = context.ProtectedNames.Include(pn => pn.Player).Include(pn => pn.CreatedByUserProfile).AsQueryable();
        var totalCount = await query.CountAsync();

        query = query.OrderBy(pn => pn.Name).Skip(skipEntries).Take(takeEntries);
        var results = await query.ToListAsync();

        var entries = results.Select(pn => mapper.Map<ProtectedNameDto>(pn)).ToList();

        var result = new ProtectedNamesCollectionDto
        {
            TotalRecords = totalCount,
            Entries = entries
        };

        return new ApiResponseDto<ProtectedNamesCollectionDto>(HttpStatusCode.OK, result);
    }

    [HttpGet]
    [Route("players/protected-names/{protectedNameId}")]
    public async Task<IActionResult> GetProtectedName(Guid protectedNameId)
    {
        var response = await ((IPlayersApi)this).GetProtectedName(protectedNameId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<ProtectedNameDto>> IPlayersApi.GetProtectedName(Guid protectedNameId)
    {
        var protectedName = await context.ProtectedNames
            .Include(pn => pn.Player)
            .Include(pn => pn.CreatedByUserProfile)
            .SingleOrDefaultAsync(pn => pn.ProtectedNameId == protectedNameId);

        if (protectedName == null)
            return new ApiResponseDto<ProtectedNameDto>(HttpStatusCode.NotFound);

        var result = mapper.Map<ProtectedNameDto>(protectedName);

        return new ApiResponseDto<ProtectedNameDto>(HttpStatusCode.OK, result);
    }

    [HttpGet]
    [Route("players/{playerId}/protected-names")]
    public async Task<IActionResult> GetProtectedNamesForPlayer(Guid playerId)
    {
        var response = await ((IPlayersApi)this).GetProtectedNamesForPlayer(playerId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<ProtectedNamesCollectionDto>> IPlayersApi.GetProtectedNamesForPlayer(Guid playerId)
    {
        if (!await context.Players.AnyAsync(p => p.PlayerId == playerId))
            return new ApiResponseDto<ProtectedNamesCollectionDto>(HttpStatusCode.NotFound);

        var query = context.ProtectedNames.Include(pn => pn.Player).Include(pn => pn.CreatedByUserProfile).Where(pn => pn.PlayerId == playerId).AsQueryable();
        var totalCount = await query.CountAsync();

        query = query.OrderBy(pn => pn.Name);
        var results = await query.ToListAsync();

        var entries = results.Select(pn => mapper.Map<ProtectedNameDto>(pn)).ToList();

        var result = new ProtectedNamesCollectionDto
        {
            TotalRecords = totalCount,
            Entries = entries
        };

        return new ApiResponseDto<ProtectedNamesCollectionDto>(HttpStatusCode.OK, result);
    }

    [HttpPost]
    [Route("players/protected-names")]
    public async Task<IActionResult> CreateProtectedName()
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        CreateProtectedNameDto? createProtectedNameDto;
        try
        {
            createProtectedNameDto = JsonConvert.DeserializeObject<CreateProtectedNameDto>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (createProtectedNameDto == null)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

        var response = await ((IPlayersApi)this).CreateProtectedName(createProtectedNameDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.CreateProtectedName(CreateProtectedNameDto createProtectedNameDto)
    {
        // Check if player exists
        if (!await context.Players.AnyAsync(p => p.PlayerId == createProtectedNameDto.PlayerId))
            return new ApiResponseDto(HttpStatusCode.NotFound, new List<string> { "Player not found" });

        // Check if the name is already protected
        if (await context.ProtectedNames.AnyAsync(pn => pn.Name.ToLower() == createProtectedNameDto.Name.ToLower()))
            return new ApiResponseDto(HttpStatusCode.Conflict, new List<string> { "This name is already protected" });

        var protectedName = new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = createProtectedNameDto.PlayerId,
            Name = createProtectedNameDto.Name,
            CreatedOn = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(createProtectedNameDto.AdminId))
            protectedName.CreatedByUserProfile = await context.UserProfiles.SingleOrDefaultAsync(u => u.XtremeIdiotsForumId == createProtectedNameDto.AdminId);

        await context.ProtectedNames.AddAsync(protectedName);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpDelete]
    [Route("players/protected-names/{protectedNameId}")]
    public async Task<IActionResult> DeleteProtectedName(Guid protectedNameId)
    {
        var deleteProtectedNameDto = new DeleteProtectedNameDto(protectedNameId);
        var response = await ((IPlayersApi)this).DeleteProtectedName(deleteProtectedNameDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto)
    {
        var protectedName = await context.ProtectedNames
            .SingleOrDefaultAsync(pn => pn.ProtectedNameId == deleteProtectedNameDto.ProtectedNameId);

        if (protectedName == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        context.ProtectedNames.Remove(protectedName);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpGet]
    [Route("players/protected-names/{protectedNameId}/usage-report")]
    public async Task<IActionResult> GetProtectedNameUsageReport(Guid protectedNameId)
    {
        var response = await ((IPlayersApi)this).GetProtectedNameUsageReport(protectedNameId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<ProtectedNameUsageReportDto>> IPlayersApi.GetProtectedNameUsageReport(Guid protectedNameId)
    {
        var protectedName = await context.ProtectedNames
            .Include(pn => pn.Player)
            .Include(pn => pn.CreatedByUserProfile)
            .FirstOrDefaultAsync(pn => pn.ProtectedNameId == protectedNameId);

        if (protectedName == null)
            return new ApiResponseDto<ProtectedNameUsageReportDto>(HttpStatusCode.NotFound);

        var owningPlayer = await context.Players
            .FirstOrDefaultAsync(p => p.PlayerId == protectedName.PlayerId);

        if (owningPlayer == null)
            return new ApiResponseDto<ProtectedNameUsageReportDto>(HttpStatusCode.NotFound);

        // Find all player aliases that match this protected name
        var matchingAliases = await context.PlayerAliases
            .Include(pa => pa.Player)
            .Where(pa => pa.Name.ToLower() == protectedName.Name.ToLower())
            .OrderByDescending(pa => pa.LastUsed)
            .ToListAsync();

        var usageInstances = new List<ProtectedNameUsageReportDto.PlayerUsageDto>();

        // Group by player and create usage instances
        foreach (var group in matchingAliases.GroupBy(a => a.PlayerId))
        {
            var player = group.First().Player;
            var isOwner = player.PlayerId == protectedName.PlayerId;

            usageInstances.Add(new ProtectedNameUsageReportDto.PlayerUsageDto
            {
                PlayerId = player.PlayerId,
                Username = player.Username,
                IsOwner = isOwner,
                LastUsed = group.Max(a => a.LastUsed),
                UsageCount = group.Count()
            });
        }

        var result = new ProtectedNameUsageReportDto
        {
            ProtectedName = mapper.Map<ProtectedNameDto>(protectedName),
            OwningPlayer = mapper.Map<PlayerDto>(owningPlayer),
            UsageInstances = usageInstances
        };

        return new ApiResponseDto<ProtectedNameUsageReportDto>(HttpStatusCode.OK, result);
    }

    #endregion
    #region Player Tags    
    [HttpGet]
    [Route("players/{playerId}/tags")]
    public async Task<IActionResult> GetPlayerTags(Guid playerId)
    {
        var response = await ((IPlayersApi)this).GetPlayerTags(playerId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<PlayerTagsCollectionDto>> IPlayersApi.GetPlayerTags(Guid playerId)
    {        // Check if the player exists
        if (!await context.Players.AnyAsync(p => p.PlayerId == playerId))
        {
            var response = new ApiResponseDto<PlayerTagsCollectionDto>(HttpStatusCode.NotFound);
            response.Errors = new List<string> { "Player not found" };
            return response;
        }

        var playerTags = await context.PlayerTags
            .Include(pt => pt.Tag)
            .Include(pt => pt.UserProfile)
            .Where(pt => pt.PlayerId == playerId)
            .ToListAsync();

        var result = new PlayerTagsCollectionDto { Entries = playerTags.Select(mapper.Map<PlayerTagDto>).ToList() };

        return new ApiResponseDto<PlayerTagsCollectionDto>(HttpStatusCode.OK, result);
    }
    [HttpPost]
    [Route("players/{playerId}/tags")]
    public async Task<IActionResult> AddPlayerTag(Guid playerId, [FromBody] PlayerTagDto playerTagDto)
    {
        // Ensure the path playerId and body playerId match
        if (playerTagDto.PlayerId != null && playerTagDto.PlayerId.Value != playerId)
        {
            return BadRequest(new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "PlayerId in the URL must match PlayerId in the request body" }));
        }

        // Set the playerId in the DTO to match the URL
        playerTagDto.PlayerId = playerId;

        var response = await ((IPlayersApi)this).AddPlayerTag(playerId, playerTagDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto)
    {
        if (playerTagDto.TagId == null)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "TagId is required" });

        // Check if the player exists
        if (!await context.Players.AnyAsync(p => p.PlayerId == playerId))
            return new ApiResponseDto(HttpStatusCode.NotFound, new List<string> { "Player not found" });

        // Check if the tag exists
        var tagExists = await context.Tags.AnyAsync(t => t.TagId == playerTagDto.TagId);
        if (!tagExists)
            return new ApiResponseDto(HttpStatusCode.NotFound, new List<string> { "Tag not found" });

        // Check if player already has this tag
        var exists = await context.PlayerTags.AnyAsync(pt => pt.PlayerId == playerId && pt.TagId == playerTagDto.TagId);
        if (exists)
            return new ApiResponseDto(HttpStatusCode.Conflict, new List<string> { "Player already has this tag" });

        var playerTag = mapper.Map<PlayerTag>(playerTagDto);
        playerTag.PlayerId = playerId;
        playerTag.Assigned = DateTime.UtcNow;

        try
        {
            context.PlayerTags.Add(playerTag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            return new ApiResponseDto(HttpStatusCode.InternalServerError, new List<string> { $"Error adding player tag: {ex.Message}" });
        }
    }
    [HttpDelete]
    [Route("players/{playerId}/tags/{playerTagId}")]
    public async Task<IActionResult> RemovePlayerTag(Guid playerId, Guid playerTagId)
    {
        var response = await ((IPlayersApi)this).RemovePlayerTag(playerId, playerTagId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.RemovePlayerTag(Guid playerId, Guid playerTagId)
    {
        // Check if the player exists
        if (!await context.Players.AnyAsync(p => p.PlayerId == playerId))
            return new ApiResponseDto(HttpStatusCode.NotFound, new List<string> { "Player not found" });

        var playerTag = await context.PlayerTags.FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId && pt.PlayerId == playerId);

        if (playerTag == null)
            return new ApiResponseDto(HttpStatusCode.NotFound, new List<string> { "Player tag not found" });

        try
        {
            context.PlayerTags.Remove(playerTag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            return new ApiResponseDto(HttpStatusCode.InternalServerError, new List<string> { $"Error removing player tag: {ex.Message}" });
        }
    }    // Helper endpoint to get a player tag by ID
    [HttpGet]
    [Route("players/tags/{playerTagId}")]
    public async Task<IActionResult> GetPlayerTagById(Guid playerTagId)
    {
        var response = await ((IPlayersApi)this).GetPlayerTagById(playerTagId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<PlayerTagDto>> IPlayersApi.GetPlayerTagById(Guid playerTagId)
    {
        var playerTag = await context.PlayerTags
            .Include(pt => pt.Player)
            .Include(pt => pt.Tag)
            .Include(pt => pt.UserProfile)
            .FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId);
        if (playerTag == null)
        {
            var response = new ApiResponseDto<PlayerTagDto>(HttpStatusCode.NotFound);
            response.Errors = new List<string> { "Player tag not found" };
            return response;
        }

        var result = mapper.Map<PlayerTagDto>(playerTag);
        return new ApiResponseDto<PlayerTagDto>(HttpStatusCode.OK, result);
    }
    [HttpDelete]
    [Route("players/tags/{playerTagId}")]
    public async Task<IActionResult> RemovePlayerTagById(Guid playerTagId)
    {
        var response = await ((IPlayersApi)this).RemovePlayerTagById(playerTagId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.RemovePlayerTagById(Guid playerTagId)
    {
        var playerTag = await context.PlayerTags
            .FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId);

        if (playerTag == null)
            return new ApiResponseDto(HttpStatusCode.NotFound, new List<string> { "Player tag not found" });

        try
        {
            context.PlayerTags.Remove(playerTag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            return new ApiResponseDto(HttpStatusCode.InternalServerError, new List<string> { $"Error removing player tag: {ex.Message}" });
        }
    }

    #endregion

    #region Players with IP Address
    [HttpGet]
    [Route("players/with-ip-address/{ipAddress}")]
    public async Task<IActionResult> GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        var response = await ((IPlayersApi)this).GetPlayersWithIpAddress(ipAddress, skipEntries, takeEntries, order, playerEntityOptions);

        return response.ToHttpResult();
    }
    async Task<ApiResponseDto<PlayersCollectionDto>> IPlayersApi.GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return new ApiResponseDto<PlayersCollectionDto>(HttpStatusCode.BadRequest);

            // Filter players related to this IP address 
            var query = context.Players
                .Include(p => p.PlayerIpAddresses.Where(i => i.Address == ipAddress))
                .Include(p => p.PlayerTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.PlayerIpAddresses.Any(i => i.Address == ipAddress));

            // Filter out test server guids
            query = query.Where(p => p.Guid != null && (!p.Guid.Contains("test") && !p.Guid.Contains("server")));

            // Apply ordering
            if (order.HasValue)
            {
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
            }
            else
            {
                query = query.OrderByDescending(p => p.LastSeen);
            }

            // Execute count query
            var totalCount = await query.CountAsync();

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
            var playerDtos = mapper.Map<List<PlayerDto>>(players);

            // Create result
            var result = new PlayersCollectionDto
            {
                TotalRecords = totalCount,
                FilteredRecords = totalCount,
                Entries = playerDtos
            };

            return new ApiResponseDto<PlayersCollectionDto>(HttpStatusCode.OK, result);
        }
        catch
        {
            return new ApiResponseDto<PlayersCollectionDto>(HttpStatusCode.InternalServerError);
        }
    }
    #endregion

    #region Player IP Addresses

    [HttpGet]
    [Route("players/{playerId}/ip-addresses")]
    public async Task<IActionResult> GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order)
    {
        var response = await ((IPlayersApi)this).GetPlayerIpAddresses(playerId, skipEntries, takeEntries, order);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<IpAddressesCollectionDto>> IPlayersApi.GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order)
    {
        var player = await context.Players.SingleOrDefaultAsync(p => p.PlayerId == playerId);

        if (player == null)
            return new ApiResponseDto<IpAddressesCollectionDto>(HttpStatusCode.NotFound);

        var query = context.PlayerIpAddresses
            .Where(pip => pip.PlayerId == playerId)
            .AsQueryable();

        // Apply ordering
        if (order.HasValue)
        {
            switch (order.Value)
            {
                case IpAddressesOrder.AddressAsc:
                    query = query.OrderBy(pip => pip.Address);
                    break;
                case IpAddressesOrder.AddressDesc:
                    query = query.OrderByDescending(pip => pip.Address);
                    break;
                case IpAddressesOrder.AddedAsc:
                    query = query.OrderBy(pip => pip.Added);
                    break;
                case IpAddressesOrder.AddedDesc:
                    query = query.OrderByDescending(pip => pip.Added);
                    break;
                case IpAddressesOrder.LastUsedAsc:
                    query = query.OrderBy(pip => pip.LastUsed);
                    break;
                case IpAddressesOrder.LastUsedDesc:
                    query = query.OrderByDescending(pip => pip.LastUsed);
                    break;
                case IpAddressesOrder.ConfidenceScoreAsc:
                    query = query.OrderBy(pip => pip.ConfidenceScore);
                    break;
                case IpAddressesOrder.ConfidenceScoreDesc:
                    query = query.OrderByDescending(pip => pip.ConfidenceScore);
                    break;
            }
        }
        else
        {
            // Default ordering
            query = query.OrderByDescending(pip => pip.LastUsed);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync();

        // Map to DTOs
        var dtos = items.Select(mapper.Map<IpAddressDto>).ToList();        // Create and return the response
        var result = new IpAddressesCollectionDto
        {
            TotalCount = totalCount,
            Entries = dtos
        };

        return new ApiResponseDto<IpAddressesCollectionDto>(HttpStatusCode.OK, result);
    }

    #endregion

    #region Player Aliases APIs

    [HttpGet]
    [Route("players/{playerId}/aliases")]
    public async Task<IActionResult> GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries)
    {
        var response = await ((IPlayersApi)this).GetPlayerAliases(playerId, skipEntries, takeEntries);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<PlayerAliasesCollectionDto>> IPlayersApi.GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries)
    {
        // Check if the player exists
        var player = await context.Players.SingleOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null)
            return new ApiResponseDto<PlayerAliasesCollectionDto>(HttpStatusCode.NotFound);

        // Get the aliases for the player
        var query = context.PlayerAliases
            .Where(pa => pa.PlayerId == playerId)
            .OrderByDescending(pa => pa.LastUsed);

        var totalCount = await query.CountAsync();

        var aliases = await query
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync();

        // Map the aliases to DTOs
        var aliasesDto = mapper.Map<List<PlayerAliasDto>>(aliases);

        var result = new PlayerAliasesCollectionDto
        {
            Entries = aliasesDto,
            TotalRecords = totalCount,
        };

        return new ApiResponseDto<PlayerAliasesCollectionDto>(HttpStatusCode.OK, result);
    }

    [HttpPost]
    [Route("players/{playerId}/aliases")]
    public async Task<IActionResult> AddPlayerAlias(Guid playerId, [FromBody] CreatePlayerAliasDto createPlayerAliasDto)
    {
        var response = await ((IPlayersApi)this).AddPlayerAlias(playerId, createPlayerAliasDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto)
    {
        // Check if the player exists
        var player = await context.Players.SingleOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

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

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpPut]
    [Route("players/{playerId}/aliases/{aliasId}")]
    public async Task<IActionResult> UpdatePlayerAlias(Guid playerId, Guid aliasId, [FromBody] CreatePlayerAliasDto updatePlayerAliasDto)
    {
        var response = await ((IPlayersApi)this).UpdatePlayerAlias(playerId, aliasId, updatePlayerAliasDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto)
    {
        // Check if the alias exists and belongs to the player
        var alias = await context.PlayerAliases
            .FirstOrDefaultAsync(pa => pa.PlayerAliasId == aliasId && pa.PlayerId == playerId);

        if (alias == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        // Update the alias
        alias.Name = updatePlayerAliasDto.Name;
        alias.LastUsed = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpDelete]
    [Route("players/{playerId}/aliases/{aliasId}")]
    public async Task<IActionResult> DeletePlayerAlias(Guid playerId, Guid aliasId)
    {
        var response = await ((IPlayersApi)this).DeletePlayerAlias(playerId, aliasId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.DeletePlayerAlias(Guid playerId, Guid aliasId)
    {
        // Check if the alias exists and belongs to the player
        var alias = await context.PlayerAliases
            .FirstOrDefaultAsync(pa => pa.PlayerAliasId == aliasId && pa.PlayerId == playerId);

        if (alias == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        // Remove the alias
        context.PlayerAliases.Remove(alias);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    [HttpGet]
    [Route("aliases/search")]
    public async Task<IActionResult> SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries)
    {
        var response = await ((IPlayersApi)this).SearchPlayersByAlias(aliasSearch, skipEntries, takeEntries);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<PlayerAliasesCollectionDto>> IPlayersApi.SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries)
    {
        if (string.IsNullOrWhiteSpace(aliasSearch) || aliasSearch.Length < 3)
            return new ApiResponseDto<PlayerAliasesCollectionDto>(HttpStatusCode.BadRequest, null, new List<string> { "Search term must be at least 3 characters long" });

        var trimmedSearch = aliasSearch.Trim();

        // Find aliases that match the search term
        var query = context.PlayerAliases
            .Where(pa => pa.Name != null && pa.Name.Contains(trimmedSearch))
            .OrderByDescending(pa => pa.LastUsed);

        var totalCount = await query.CountAsync();

        var aliases = await query
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync();

        // Map the aliases to DTOs
        var aliasesDto = mapper.Map<List<PlayerAliasDto>>(aliases);

        var result = new PlayerAliasesCollectionDto
        {
            Entries = aliasesDto,
            TotalRecords = totalCount,
        };

        return new ApiResponseDto<PlayerAliasesCollectionDto>(HttpStatusCode.OK, result);
    }
}

#endregion