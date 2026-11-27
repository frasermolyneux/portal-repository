using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using Xunit;
using Xunit.Abstractions;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ChatIndexSqlTests(ITestOutputHelper output)
{
    private const string IndexName = "IX_ChatMessages_Timestamp_Locked";

    [Fact]
    [Trait("Category", "SqlIntegration")]
    public Task CoveringIndex_PreservesChatAggregatesAndReducesShortWindowReads() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).Options);
            var servers = Enumerable.Range(0, 3).Select(i => new GameServer
            {
                GameServerId = Guid.NewGuid(),
                Title = $"Server {i}",
                Hostname = "host",
                BanFileRootPath = "",
                GameType = 2
            }).ToArray();
            var end = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var players = Enumerable.Range(0, 100).Select(i => new Player
            {
                PlayerId = Guid.NewGuid(),
                Username = $"Player {i}",
                GameType = i < 25 ? 2 : 24,
                FirstSeen = end.AddDays(-224),
                LastSeen = end
            }).ToArray();
            context.AddRange(servers);
            context.AddRange(players);
            await context.SaveChangesAsync();
            await context.Database.OpenConnectionAsync();
            var connection = (SqlConnection)context.Database.GetDbConnection();
            using var rows = BuildRows(100_000, end, servers, players);

            // EnsureCreated does not reproduce DACPAC INCLUDE columns.
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "ChatMessages.sql"));
            var indexes = Regex.Split(script, @"(?im)^\s*GO\s*$")
                .Where(batch => Regex.IsMatch(batch, @"^\s*CREATE NONCLUSTERED INDEX")).ToArray();
            var candidate = Assert.Single(indexes, batch => batch.Contains($"[{IndexName}]", StringComparison.Ordinal));
            foreach (var batch in indexes)
            {
                await ReplaceIndex(connection, batch.Replace(", [PlayerId], [GameServerId]", "", StringComparison.Ordinal));
            }

            await Insert(connection, rows);

            var queries = new Dictionary<string, string>
            {
                ["global"] = "SELECT TOP (10) c.PlayerId, COUNT(*) AS Messages, MAX(c.Timestamp) AS LastSeen FROM dbo.ChatMessages c WHERE c.Timestamp >= @from AND c.Timestamp < @to GROUP BY c.PlayerId ORDER BY COUNT(*) DESC",
                ["game"] = "SELECT TOP (10) c.PlayerId, c.Username, COUNT(*) AS Messages, MAX(c.Timestamp) AS LastSeen FROM dbo.ChatMessages c JOIN dbo.Players p ON p.PlayerId = c.PlayerId WHERE c.Timestamp >= @from AND c.Timestamp < @to AND p.GameType = 2 GROUP BY c.PlayerId, c.Username ORDER BY COUNT(*) DESC",
                ["server-count"] = "SELECT COUNT(*) FROM dbo.ChatMessages c WHERE c.Timestamp >= @from AND c.Timestamp < @to AND c.GameServerId = @server",
                ["server-projection"] = "SELECT c.PlayerId, c.Username FROM dbo.ChatMessages c WHERE c.Timestamp >= @from AND c.Timestamp < @to AND c.GameServerId = @server"
            };
            queries["minority-server-count"] = queries["server-count"];
            queries["minority-server-projection"] = queries["server-projection"];
            var baseline = new Dictionary<(string Name, int Days), QueryEvidence>();
            foreach (var days in new[] { 1, 7, 30 })
            {
                foreach (var (name, sql) in queries)
                {
                    var server = servers[name.StartsWith("minority", StringComparison.Ordinal) ? 1 : 0].GameServerId;
                    baseline[(name, days)] = await Measure(connection, sql, end.AddDays(-days), end,
                        server);
                    EmitPlans("baseline", name, days, server, end, baseline[(name, days)]);
                }
            }

            var originalSize = await IndexSize(connection);
            var originalWrite = await MeasureInsert(connection, rows);

            await ReplaceIndex(connection, candidate);
            var coveredSize = await IndexSize(connection);
            var coveredWrite = await MeasureInsert(connection, rows);
            output.WriteLine($"Synthetic rows={rows.Rows.Count}; index KiB before={originalSize}, after={coveredSize}; 1000-row insert log bytes/ms before={originalWrite}, after={coveredWrite}");
            foreach (var days in new[] { 1, 7, 30 })
            {
                foreach (var (name, sql) in queries)
                {
                    var before = baseline[(name, days)];
                    var server = servers[name.StartsWith("minority", StringComparison.Ordinal) ? 1 : 0].GameServerId;
                    var after = await Measure(connection, sql, end.AddDays(-days), end,
                        server);
                    output.WriteLine($"{name} {days}d total logical reads before={before.ReadsByTable.Values.Sum()}, after={after.ReadsByTable.Values.Sum()}; per-table data+LOB pages before={string.Join(",", before.ReadsByTable)}, after={string.Join(",", after.ReadsByTable)}; retrieval ms before={before.ElapsedMs}, after={after.ElapsedMs}; chat accesses before={string.Join(",", before.Accesses)}, after={string.Join(",", after.Accesses)}");
                    EmitPlans("candidate", name, days, server, end, after);
                    Assert.Equal(before.Rows, after.Rows);
                    if (name == "global" && days == 1)
                    {
                        Assert.True(after.ReadsByTable["ChatMessages"] < before.ReadsByTable["ChatMessages"],
                            $"One-day global ChatMessages logical reads: {before.ReadsByTable["ChatMessages"]} -> {after.ReadsByTable["ChatMessages"]}");
                        Assert.Contains(after.Accesses, access => access.Contains(IndexName, StringComparison.Ordinal));
                        Assert.DoesNotContain(after.Accesses, access => access.Contains("Key Lookup", StringComparison.Ordinal));
                    }
                }
            }
        });

    private void EmitPlans(string phase, string query, int days, Guid server, DateTime end, QueryEvidence evidence)
    {
        var scope = query.Contains("server", StringComparison.Ordinal) ? server.ToString() : "all";
        foreach (var plan in evidence.Plans)
        {
            output.WriteLine($"SQL PLAN {phase}: query={query}; from={end.AddDays(-days):O}; to={end:O}; server={scope}");
            output.WriteLine(plan);
        }
    }

    private static DataTable BuildRows(int count, DateTime end, GameServer[] servers, Player[] players)
    {
        var rows = new DataTable();
        rows.Columns.Add("GameServerId", typeof(Guid));
        rows.Columns.Add("PlayerId", typeof(Guid));
        rows.Columns.Add("Username", typeof(string));
        rows.Columns.Add("ChatType", typeof(int));
        rows.Columns.Add("Message", typeof(string));
        rows.Columns.Add("Timestamp", typeof(DateTime));
        rows.Columns.Add("Locked", typeof(bool));
        for (var i = 0; i < count; i++)
        {
            var selector = i % 5050;
            var player = 0;
            while (selector >= player + 1)
            {
                selector -= ++player;
            }

            var server = i % 10 < 8 ? 0 : i % 10 - 7;
            rows.Rows.Add(servers[server].GameServerId, players[player].PlayerId, $"P{player:D3}-alias{(player == 0 ? i / 5050 % 2 : 0)}",
                0, "recorded chat", end.AddSeconds(-1 - (long)i * 224 * 86400 / count), false);
        }
        return rows;
    }

    private static async Task ReplaceIndex(SqlConnection connection, string batch)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = batch.Trim().TrimEnd(';') + " WITH (DROP_EXISTING = ON);";
        command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task Insert(SqlConnection connection, DataTable rows, SqlTransaction? transaction = null)
    {
        using var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction)
        {
            DestinationTableName = "dbo.ChatMessages",
            BulkCopyTimeout = 60
        };
        foreach (DataColumn column in rows.Columns)
        {
            copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await copy.WriteToServerAsync(rows);
    }

    private static async Task<long> IndexSize(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SUM(used_page_count) * 8 FROM sys.dm_db_partition_stats WHERE object_id = OBJECT_ID(N'dbo.ChatMessages') AND index_id = INDEXPROPERTY(OBJECT_ID(N'dbo.ChatMessages'), 'IX_ChatMessages_Timestamp_Locked', 'IndexId')";
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<(long LogBytes, long ElapsedMs)> MeasureInsert(SqlConnection connection, DataTable rows)
    {
        using var batch = rows.Clone();
        foreach (DataRow row in rows.Rows.Cast<DataRow>().Take(1000))
        {
            batch.ImportRow(row);
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            var watch = Stopwatch.StartNew();
            await Insert(connection, batch, transaction);
            watch.Stop();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT database_transaction_log_bytes_used FROM sys.dm_tran_database_transactions d JOIN sys.dm_tran_session_transactions s ON s.transaction_id = d.transaction_id WHERE s.session_id = @@SPID AND d.database_id = DB_ID()";
            return (Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture), watch.ElapsedMilliseconds);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task<QueryEvidence> Measure(SqlConnection connection, string sql, DateTime from, DateTime to, Guid server)
    {
        var messages = new List<string>();
        SqlInfoMessageEventHandler handler = (_, args) => messages.Add(args.Message);
        connection.InfoMessage += handler;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SET STATISTICS IO ON; SET STATISTICS XML ON; {sql}; SET STATISTICS XML OFF; SET STATISTICS IO OFF;";
            command.CommandTimeout = 60;
            command.Parameters.Add("@from", SqlDbType.DateTime).Value = from;
            command.Parameters.Add("@to", SqlDbType.DateTime).Value = to;
            command.Parameters.Add("@server", SqlDbType.UniqueIdentifier).Value = server;
            var rawRows = new List<object[]>();
            var plans = new List<string>();
            var accesses = new List<string>();
            var watch = Stopwatch.StartNew();
            await using var reader = await command.ExecuteReaderAsync();
            do
            {
                while (await reader.ReadAsync())
                {
                    if (reader.FieldCount == 1 && (reader.GetDataTypeName(0) == "xml"
                        || reader.GetName(0).Contains("Showplan", StringComparison.OrdinalIgnoreCase)))
                    {
                        plans.Add(reader.GetString(0));
                    }
                    else
                    {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        rawRows.Add(values);
                    }
                }
            } while (await reader.NextResultAsync());
            watch.Stop();
            var rows = rawRows.Select(values => string.Join("|", values
                .Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))))
                .Order(StringComparer.Ordinal).ToList();
            XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
            foreach (var xml in plans)
            {
                foreach (var operation in XDocument.Parse(xml).Descendants(ns + "RelOp"))
                {
                    foreach (var obj in operation.Elements(ns + "IndexScan").Elements(ns + "Object")
                        .Where(obj => (string?)obj.Attribute("Table") == "[ChatMessages]"))
                    {
                        accesses.Add($"{operation.Attribute("LogicalOp")?.Value}:{obj.Attribute("Index")?.Value}");
                    }
                }
            }
            var readsByTable = messages.SelectMany(message => Regex.Matches(message,
                    @"Table '([^']+)'.*?logical reads (\d+).*?lob logical reads (\d+)").Cast<Match>())
                .GroupBy(match => match.Groups[1].Value, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Sum(match =>
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
                    + int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)), StringComparer.Ordinal);
            Assert.Contains("ChatMessages", readsByTable.Keys);
            Assert.NotEmpty(plans);
            return new QueryEvidence(rows, readsByTable, watch.ElapsedMilliseconds, accesses, plans);
        }
        finally
        {
            connection.InfoMessage -= handler;
        }
    }

    private sealed record QueryEvidence(List<string> Rows, Dictionary<string, int> ReadsByTable, long ElapsedMs,
        List<string> Accesses, List<string> Plans);
}
