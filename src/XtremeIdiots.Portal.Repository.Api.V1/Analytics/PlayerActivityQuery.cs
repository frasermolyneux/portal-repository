using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Analytics;

internal static class PlayerActivityQuery
{
    public static async Task<PlayerActivityDto> ReadAsync(PortalDbContext context, Guid playerId, int playerGame,
        DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<Guid> authorizedServerIds, bool includeChat,
        CancellationToken cancellationToken)
    {
        var servers = await context.GameServers
            .Where(s => !s.Deleted && authorizedServerIds.Contains(s.GameServerId))
            .Select(s => new { s.GameServerId, s.Title, s.GameType })
            .ToDictionaryAsync(s => s.GameServerId, cancellationToken).ConfigureAwait(false);
        if (servers.Count == 0)
        {
            return new PlayerActivityDto { FromUtc = fromUtc, ToUtc = toUtc, PlaytimeCoverage = "unavailable" };
        }

        var scope = servers.Keys.ToArray();
        var sessions = context.PlayerSessions
            .Where(s => s.PlayerId == playerId && scope.Contains(s.ServerId) && s.IsBot != true);
        var tracked = await sessions
            .Where(s => s.StartUtc < toUtc && (s.EndUtc ?? s.LastConfirmedUtc) > fromUtc)
            .GroupBy(s => s.ServerId)
            .Select(g => new { ServerId = g.Key, Count = g.Count(), HasOpen = g.Any(s => s.EndUtc == null) })
            .ToDictionaryAsync(s => s.ServerId, cancellationToken).ConfigureAwait(false);

        var playtime = await ReadPlaytimeAsync(context, playerId, scope, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        var daily = playtime.Where(p => p.MeasuredNanoseconds > 0)
            .GroupBy(p => p.DateUtc)
            .OrderBy(g => g.Key)
            .Select(g => new PlayerActivityDayDto
            {
                DateUtc = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                MeasuredSeconds = Seconds(g.Sum(p => p.MeasuredNanoseconds))
            }).ToArray();
        var measuredByServer = playtime.GroupBy(p => p.ServerId)
            .ToDictionary(g => g.Key, g => Seconds(g.Sum(p => p.MeasuredNanoseconds)));
        var serverBreakdown = tracked.Keys.Union(measuredByServer.Keys)
            .Select(id => new PlayerActivityServerDto
            {
                ServerId = id,
                ServerName = servers[id].Title,
                MeasuredSeconds = measuredByServer.GetValueOrDefault(id),
                TrackedSessions = tracked.GetValueOrDefault(id)?.Count ?? 0
            })
            .OrderByDescending(s => s.MeasuredSeconds)
            .ThenByDescending(s => s.TrackedSessions)
            .ThenBy(s => s.ServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.ServerId)
            .ToArray();

        var lastConfirmed = await sessions.Where(s => s.LastConfirmedUtc <= toUtc)
            .OrderByDescending(s => s.LastConfirmedUtc).Select(s => (DateTime?)s.LastConfirmedUtc)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var lastRecent = await context.RecentPlayers
            .Where(r => r.PlayerId == playerId && r.GameServerId.HasValue && scope.Contains(r.GameServerId.Value)
                && r.Timestamp <= toUtc)
            .OrderByDescending(r => r.Timestamp).Select(r => (DateTime?)r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var useRecent = lastRecent.HasValue && (!lastConfirmed.HasValue || lastRecent.Value > lastConfirmed.Value);
        var lastObserved = useRecent ? lastRecent : lastConfirmed;

        long? chatMessages = null;
        DateTime? chatCutoffUtc = null;
        if (includeChat)
        {
            var chatScope = servers.Values.Where(s => s.GameType == playerGame).Select(s => s.GameServerId).ToArray();
            if (chatScope.Length > 0)
            {
                var cutoff = DateTime.UtcNow.AddMonths(-HistoryRetention.ChatMessageMonths);
                var chat = context.ChatMessages.Where(m => m.PlayerId == playerId
                    && chatScope.Contains(m.GameServerId) && m.Timestamp >= fromUtc && m.Timestamp < toUtc);
                // Recent windows are already inside retention; avoid fetching Locked from the clustered row.
                if (fromUtc < cutoff)
                {
                    chat = chat.Where(m => m.Timestamp >= cutoff || m.Locked);
                }
                chatMessages = await chat.LongCountAsync(cancellationToken).ConfigureAwait(false);
                chatCutoffUtc = cutoff;
            }
        }

        return new PlayerActivityDto
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            MeasuredSeconds = daily.Sum(d => d.MeasuredSeconds),
            UnknownMapSeconds = Seconds(playtime.Sum(p => p.UnknownNanoseconds)),
            TrackedSessions = tracked.Values.Sum(s => s.Count),
            ActiveDays = daily.Length,
            HasOpenSession = tracked.Values.Any(s => s.HasOpen),
            ChatMessages = chatMessages,
            ChatRetentionCutoffUtc = chatCutoffUtc,
            LastObservedUtc = lastObserved.HasValue ? DateTime.SpecifyKind(lastObserved.Value, DateTimeKind.Utc) : null,
            LastObservedSource = lastObserved.HasValue ? (useRecent ? "recentPlayer" : "session") : null,
            Daily = daily,
            Servers = serverBreakdown
        };
    }

    private static decimal Seconds(decimal nanoseconds) => nanoseconds / 1_000_000_000m;

    private static Task<List<DailyPlaytime>> ReadPlaytimeAsync(PortalDbContext context, Guid playerId, Guid[] scope,
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        context.Database.SqlQueryRaw<DailyPlaytime>(
            """
            WITH days AS
            (
                SELECT CONVERT(datetime2, CONVERT(date, @FromUtc)) AS DateUtc
                UNION ALL
                SELECT DATEADD(day, 1, DateUtc) FROM days WHERE DATEADD(day, 1, DateUtc) < @ToUtc
            ),
            clipped AS
            (
                SELECT s.ServerId, m.UnknownMap,
                       CASE WHEN m.StartUtc < @FromUtc THEN @FromUtc ELSE m.StartUtc END AS StartUtc,
                       CASE WHEN m.EndUtc > @ToUtc THEN @ToUtc ELSE m.EndUtc END AS EndUtc
                FROM dbo.PlayerSession AS s
                JOIN OPENJSON(@ServerIds) WITH (ServerId uniqueidentifier '$') AS scope ON scope.ServerId = s.ServerId
                JOIN dbo.PlayerSessionMapSegment AS m ON m.SessionId = s.SessionId
                WHERE s.PlayerId = @PlayerId AND (s.IsBot = 0 OR s.IsBot IS NULL)
                  AND m.StartUtc < @ToUtc AND m.EndUtc > @FromUtc
            )
            SELECT d.DateUtc, c.ServerId,
                   SUM(CASE WHEN c.UnknownMap = 0 THEN elapsed.Nanoseconds ELSE 0 END) AS MeasuredNanoseconds,
                   SUM(CASE WHEN c.UnknownMap = 1 THEN elapsed.Nanoseconds ELSE 0 END) AS UnknownNanoseconds
            FROM clipped AS c
            JOIN days AS d ON c.StartUtc < DATEADD(day, 1, d.DateUtc) AND c.EndUtc > d.DateUtc
            CROSS APPLY (VALUES (CONVERT(decimal(38, 0), DATEDIFF_BIG(nanosecond,
                CASE WHEN c.StartUtc > d.DateUtc THEN c.StartUtc ELSE d.DateUtc END,
                CASE WHEN c.EndUtc < DATEADD(day, 1, d.DateUtc) THEN c.EndUtc ELSE DATEADD(day, 1, d.DateUtc) END))))
                AS elapsed(Nanoseconds)
            GROUP BY d.DateUtc, c.ServerId
            """,
            new SqlParameter("@PlayerId", SqlDbType.UniqueIdentifier) { Value = playerId },
            new SqlParameter("@ServerIds", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(scope) },
            new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc },
            new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc })
            .ToListAsync(cancellationToken);

    private sealed class DailyPlaytime
    {
        public DateTime DateUtc { get; set; }
        public Guid ServerId { get; set; }
        public decimal MeasuredNanoseconds { get; set; }
        public decimal UnknownNanoseconds { get; set; }
    }
}
