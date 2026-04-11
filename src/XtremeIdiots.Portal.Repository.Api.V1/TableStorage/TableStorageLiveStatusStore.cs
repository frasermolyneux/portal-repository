using Azure;
using Azure.Data.Tables;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.TableStorage;

public class TableStorageLiveStatusStore : ILiveStatusStore
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(5);

    private readonly TableClient _statusTableClient;
    private readonly TableClient _playersTableClient;

    public TableStorageLiveStatusStore(TableServiceClient tableServiceClient)
    {
        ArgumentNullException.ThrowIfNull(tableServiceClient);

        _statusTableClient = tableServiceClient.GetTableClient("GameServerLiveStatus");
        _playersTableClient = tableServiceClient.GetTableClient("GameServerLivePlayers");
    }

    public async Task SetServerLiveStatusAsync(Guid serverId, GameServerLiveStatusEntity entity, CancellationToken cancellationToken = default)
    {
        entity.PartitionKey = "live";
        entity.RowKey = serverId.ToString();

        await _statusTableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GameServerLiveStatusDto?> GetServerLiveStatusAsync(Guid serverId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _statusTableClient.GetEntityAsync<GameServerLiveStatusEntity>(
                "live", serverId.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);

            return MapToDto(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<GameServerLiveStatusDto>> GetAllServerLiveStatusesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<GameServerLiveStatusDto>();

        await foreach (var entity in _statusTableClient.QueryAsync<GameServerLiveStatusEntity>(
            filter: $"PartitionKey eq 'live'", cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            results.Add(MapToDto(entity));
        }

        return results;
    }

    public async Task SetLivePlayersAsync(Guid serverId, List<GameServerLivePlayerEntity> players, CancellationToken cancellationToken = default)
    {
        var partitionKey = serverId.ToString();

        // Delete existing players for this server
        var existingEntities = new List<TableEntity>();
        await foreach (var entity in _playersTableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{partitionKey}'",
            select: new[] { "PartitionKey", "RowKey" },
            cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            entity.ETag = ETag.All;
            existingEntities.Add(entity);
        }

        // Batch delete existing entities (max 100 per batch)
        foreach (var batch in existingEntities.Chunk(100))
        {
            var deleteBatch = new List<TableTransactionAction>();
            foreach (var entity in batch)
            {
                deleteBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
            }

            if (deleteBatch.Count > 0)
            {
                await _playersTableClient.SubmitTransactionAsync(deleteBatch, cancellationToken).ConfigureAwait(false);
            }
        }

        // Batch insert new players, deduplicated by slot number (max 100 per batch)
        var deduplicatedPlayers = players
            .GroupBy(p => p.Num)
            .Select(g => g.Last())
            .ToList();

        foreach (var batch in deduplicatedPlayers.Chunk(100))
        {
            var insertBatch = new List<TableTransactionAction>();
            foreach (var player in batch)
            {
                player.PartitionKey = partitionKey;
                // Use slot number as RowKey for uniqueness within a server
                player.RowKey = player.Num.ToString();
                insertBatch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, player));
            }

            if (insertBatch.Count > 0)
            {
                await _playersTableClient.SubmitTransactionAsync(insertBatch, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<List<LivePlayerDto>> GetLivePlayersAsync(Guid serverId, CancellationToken cancellationToken = default)
    {
        var partitionKey = serverId.ToString();
        var results = new List<LivePlayerDto>();

        await foreach (var entity in _playersTableClient.QueryAsync<GameServerLivePlayerEntity>(
            filter: $"PartitionKey eq '{partitionKey}'", cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            results.Add(MapPlayerToDto(entity, serverId));
        }

        return results;
    }

    private static GameServerLiveStatusDto MapToDto(GameServerLiveStatusEntity entity)
    {
        var lastUpdated = entity.LastUpdated;
        var isOnline = (DateTime.UtcNow - lastUpdated) < OnlineThreshold;

        return new GameServerLiveStatusDto
        {
            ServerId = Guid.TryParse(entity.RowKey, out var id) ? id : Guid.Empty,
            Title = entity.Title,
            Map = entity.Map,
            Mod = entity.Mod,
            MaxPlayers = entity.MaxPlayers,
            CurrentPlayers = entity.CurrentPlayers,
            LastUpdated = lastUpdated,
            IsOnline = isOnline
        };
    }

    private static LivePlayerDto MapPlayerToDto(GameServerLivePlayerEntity entity, Guid serverId)
    {
        return new LivePlayerDto
        {
            LivePlayerId = Guid.NewGuid(),
            Name = entity.Name,
            Score = entity.Score,
            Ping = entity.Ping,
            Num = entity.Num,
            Rate = entity.Rate,
            Team = entity.Team,
            Time = TimeSpan.TryParse(entity.Time, out var time) ? time : TimeSpan.Zero,
            IpAddress = entity.IpAddress,
            Lat = entity.Lat,
            Long = entity.Long,
            CountryCode = entity.CountryCode,
            GameType = entity.GameType.ToGameType(),
            PlayerId = entity.PlayerId,
            GameServerServerId = serverId
        };
    }
}
