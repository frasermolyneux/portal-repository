using System.Net;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public PlayersController(
        PortalDbContext context,
        IMapper mapper)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
        var query = context.Players.AsQueryable();

        query = ApplyFilter(query, gameType, null, null);
        var totalCount = await query.CountAsync();

        query = ApplyFilter(query, gameType, filter, filterString);
        var filteredCount = await query.CountAsync();

        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
        var results = await query.ToListAsync();

        var playerIds = results.Select(e => (Guid?)e.PlayerId).ToList();

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.Aliases))
        {
            var playerAliases = await context.PlayerAliases.Where(pa => playerIds.Contains(pa.PlayerId)).ToListAsync();

            results.ForEach(player =>
            {
                player.PlayerAliases = playerAliases.Where(a => a.PlayerId == player.PlayerId).ToList();
            });
        }

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.IpAddresses))
        {
            var playerIpAddresses = await context.PlayerIpAddresses.Where(pip => playerIds.Contains(pip.PlayerId)).ToListAsync();

            results.ForEach(player =>
            {
                player.PlayerIpAddresses = playerIpAddresses.Where(a => a.PlayerId == player.PlayerId).ToList();
            });
        }

        if (playerEntityOptions.HasFlag(PlayerEntityOptions.AdminActions))
        {
            var adminActions = await context.AdminActions.Where(aa => playerIds.Contains(aa.PlayerId)).ToListAsync();

            results.ForEach(player =>
            {
                player.AdminActions = adminActions.Where(a => a.PlayerId == player.PlayerId).ToList();
            });
        }

        var entries = results.Select(p => mapper.Map<PlayerDto>(p)).ToList();

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
            query = query.Where(p => p.GameType == gameType.Value.ToGameTypeInt()).AsQueryable();

        if (filter.HasValue && !string.IsNullOrWhiteSpace(filterString))
        {
            switch (filter)
            {
                case PlayersFilter.UsernameAndGuid:
                    if (filterString.Length == 32) // Search is for a player GUID; perform a smart query
                        query = query.Where(p => p.Guid == filterString).AsQueryable();
                    else
                        query = query.Where(p => p.Username.Contains(filterString) || p.Guid.Contains(filterString) || p.PlayerAliases.Any(a => a.Name.Contains(filterString))).AsQueryable();

                    break;
                case PlayersFilter.IpAddress:
                    if (IPAddress.TryParse(filterString, out IPAddress? filterIpAddress)) // Search is for an IP Address; perform a smart query
                        query = query.Where(p => p.IpAddress == filterString || p.PlayerIpAddresses.Any(ip => ip.Address == filterString)).AsQueryable();
                    else
                        query = query.Where(p => p.IpAddress.Contains(filterString) || p.PlayerIpAddresses.Any(ip => ip.Address.Contains(filterString))).AsQueryable();

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
    {
        var playerTags = await context.PlayerTags
            .Include(pt => pt.Tag)
            .Where(pt => pt.PlayerId == playerId)
            .ToListAsync();

        var result = new PlayerTagsCollectionDto { Entries = playerTags.Select(mapper.Map<PlayerTagDto>).ToList() };

        return new ApiResponseDto<PlayerTagsCollectionDto>(HttpStatusCode.OK, result);
    }

    [HttpPost]
    [Route("players/{playerId}/tags")]
    public async Task<IActionResult> AddPlayerTag(Guid playerId, [FromBody] PlayerTagDto playerTagDto)
    {
        var response = await ((IPlayersApi)this).AddPlayerTag(playerId, playerTagDto);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IPlayersApi.AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto)
    {
        if (playerTagDto.TagId == null)
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "TagId is required" });

        var exists = await context.PlayerTags.AnyAsync(pt => pt.PlayerId == playerId && pt.TagId == playerTagDto.TagId);

        if (exists)
            return new ApiResponseDto(HttpStatusCode.Conflict, new List<string> { "Player already has this tag" });

        var playerTag = mapper.Map<PlayerTag>(playerTagDto);
        playerTag.PlayerId = playerId;
        playerTag.Assigned = DateTime.UtcNow;

        context.PlayerTags.Add(playerTag);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
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
        var playerTag = await context.PlayerTags.FirstOrDefaultAsync(pt => pt.PlayerTagId == playerTagId && pt.PlayerId == playerId);

        if (playerTag == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        context.PlayerTags.Remove(playerTag);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    #endregion
}