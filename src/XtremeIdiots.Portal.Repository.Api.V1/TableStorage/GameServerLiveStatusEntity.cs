using Azure;
using Azure.Data.Tables;

namespace XtremeIdiots.Portal.Repository.Api.V1.TableStorage;

public class GameServerLiveStatusEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "live";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string? Title { get; set; }
    public string? Map { get; set; }
    public string? Mod { get; set; }
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public DateTime LastUpdated { get; set; }
}
