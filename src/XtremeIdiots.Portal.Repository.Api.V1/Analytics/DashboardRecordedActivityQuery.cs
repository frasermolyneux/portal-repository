using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Analytics;

internal static class DashboardRecordedActivityQuery
{
    public static async Task<List<DashboardRecordedActivityPointDto>> ReadAsync(
        PortalDbContext context, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken,
        Guid? serverId = null)
    {
        var buckets = AnalyticsTimeBucketing.BuildBuckets(fromUtc, toUtc, bucket)
            .Select(start => new Bucket
            {
                Start = start,
                End = AnalyticsTimeBucketing.Add(start, bucket),
                From = start < fromUtc ? fromUtc : start,
                To = AnalyticsTimeBucketing.Add(start, bucket) > toUtc ? toUtc : AnalyticsTimeBucketing.Add(start, bucket)
            }).ToList();

        if (!context.Database.IsSqlServer())
        {
            return await ReadInMemoryAsync(context, buckets, fromUtc, toUtc, serverId, cancellationToken).ConfigureAwait(false);
        }

        var parameters = new[]
        {
            new SqlParameter("@Buckets", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(buckets) },
            new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc },
            new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc },
            new SqlParameter("@ServerId", SqlDbType.UniqueIdentifier) { Value = (object?)serverId ?? DBNull.Value }
        };
        var rows = await context.Database.SqlQueryRaw<ActivityRow>(
            """
            WITH buckets AS
            (
                SELECT [Start], [End], [From], [To]
                FROM OPENJSON(@Buckets) WITH ([Start] datetime2 '$.Start', [End] datetime2 '$.End',
                    [From] datetime2 '$.From', [To] datetime2 '$.To')
            ),
            sessions AS
            (
                SELECT s.PlayerId, s.StartUtc, s.LastConfirmedUtc, COALESCE(s.EndUtc, s.LastConfirmedUtc) AS EndUtc
                FROM dbo.PlayerSession AS s
                JOIN dbo.GameServers AS server ON server.GameServerId = s.ServerId AND server.Deleted = 0
                WHERE (s.IsBot = 0 OR s.IsBot IS NULL) AND (@ServerId IS NULL OR s.ServerId = @ServerId) AND s.StartUtc < @ToUtc
                  AND (COALESCE(s.EndUtc, s.LastConfirmedUtc) > @FromUtc OR s.LastConfirmedUtc > @FromUtc OR s.StartUtc >= @FromUtc)
            ),
            counts AS
            (
                SELECT b.[Start], COUNT(DISTINCT CASE WHEN s.StartUtc < b.[To] AND s.EndUtc > b.[From]
                    THEN s.PlayerId END) AS RecordedPlayers,
                    COUNT(CASE WHEN s.StartUtc >= b.[From] AND s.StartUtc < b.[To] THEN 1 END) AS SessionStarts,
                    SUM(CASE WHEN s.LastConfirmedUtc > s.StartUtc AND s.LastConfirmedUtc > b.[From]
                        AND s.StartUtc < b.[To] THEN CONVERT(decimal(38, 0), DATEDIFF_BIG(nanosecond,
                            CASE WHEN s.StartUtc > b.[From] THEN s.StartUtc ELSE b.[From] END,
                            CASE WHEN s.LastConfirmedUtc < b.[To] THEN s.LastConfirmedUtc ELSE b.[To] END))
                        ELSE CONVERT(decimal(38, 0), 0) END) AS ObservedNanoseconds
                FROM buckets AS b LEFT JOIN sessions AS s ON s.StartUtc < b.[To]
                    AND (s.EndUtc > b.[From] OR s.LastConfirmedUtc > b.[From] OR s.StartUtc >= b.[From])
                GROUP BY b.[Start]
            )
            SELECT b.[Start] AS BucketStartUtc, b.[End] AS BucketEndUtc, c.RecordedPlayers, c.SessionStarts,
                c.ObservedNanoseconds
            FROM buckets AS b JOIN counts AS c ON c.[Start] = b.[Start]
            ORDER BY b.[Start]
            """, parameters).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(row => new DashboardRecordedActivityPointDto
        {
            BucketStartUtc = DateTime.SpecifyKind(row.BucketStartUtc, DateTimeKind.Utc),
            BucketEndUtc = DateTime.SpecifyKind(row.BucketEndUtc, DateTimeKind.Utc),
            RecordedPlayers = row.RecordedPlayers,
            SessionStarts = row.SessionStarts,
            ObservedSeconds = row.ObservedNanoseconds / 1_000_000_000m
        }).ToList();
    }

    private static async Task<List<DashboardRecordedActivityPointDto>> ReadInMemoryAsync(
        PortalDbContext context, List<Bucket> buckets, DateTime fromUtc, DateTime toUtc, Guid? serverId, CancellationToken cancellationToken)
    {
        var sessions = await context.PlayerSessions.AsNoTracking()
            .Where(s => !s.Server.Deleted && s.IsBot != true && (serverId == null || s.ServerId == serverId) && s.StartUtc < toUtc
                && ((s.EndUtc ?? s.LastConfirmedUtc) > fromUtc || s.LastConfirmedUtc > fromUtc || s.StartUtc >= fromUtc))
            .Select(s => new { s.PlayerId, s.StartUtc, s.LastConfirmedUtc, EndUtc = s.EndUtc ?? s.LastConfirmedUtc })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return buckets.Select(b =>
        {
            var observedSeconds = sessions.Where(s => s.LastConfirmedUtc > s.StartUtc && s.LastConfirmedUtc > b.From && s.StartUtc < b.To)
                .Sum(s => (decimal)((s.LastConfirmedUtc < b.To ? s.LastConfirmedUtc : b.To) - (s.StartUtc > b.From ? s.StartUtc : b.From)).Ticks)
                / TimeSpan.TicksPerSecond;
            return new DashboardRecordedActivityPointDto
            {
                BucketStartUtc = b.Start,
                BucketEndUtc = b.End,
                RecordedPlayers = sessions.Where(s => s.StartUtc < b.To && s.EndUtc > b.From)
                    .Select(s => s.PlayerId).Distinct().Count(),
                SessionStarts = sessions.Count(s => s.StartUtc >= b.From && s.StartUtc < b.To),
                ObservedSeconds = observedSeconds
            };
        }).ToList();
    }

    private sealed class Bucket
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    private sealed class ActivityRow
    {
        public DateTime BucketStartUtc { get; set; }
        public DateTime BucketEndUtc { get; set; }
        public int RecordedPlayers { get; set; }
        public int SessionStarts { get; set; }
        public decimal ObservedNanoseconds { get; set; }
    }
}
