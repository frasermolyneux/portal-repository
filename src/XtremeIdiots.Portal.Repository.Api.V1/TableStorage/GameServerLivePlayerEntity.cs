using Azure;
using Azure.Data.Tables;

namespace XtremeIdiots.Portal.Repository.Api.V1.TableStorage;

public class GameServerLivePlayerEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public Guid? PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Ping { get; set; }
    public int Num { get; set; }
    public int Rate { get; set; }
    public string? Team { get; set; }
    public string Time { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime? ConnectedAtUtc { get; set; }
    public int GameType { get; set; }
    public string? GeoIntelligenceJson { get; set; }
}
