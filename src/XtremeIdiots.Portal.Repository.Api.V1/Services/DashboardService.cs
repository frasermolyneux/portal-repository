using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Default (uncached) implementation of <see cref="IDashboardService"/>. Behaviour is a
    /// faithful port of the pre-refactor <c>DashboardController</c> logic so the caching
    /// decorator is transparent.
    /// </summary>
    public sealed class DashboardService : IDashboardService
    {
        private readonly PortalDbContext context;
        private readonly ILiveStatusStore liveStatusStore;

        public DashboardService(PortalDbContext context, ILiveStatusStore liveStatusStore)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(liveStatusStore);
            this.context = context;
            this.liveStatusStore = liveStatusStore;
        }

        public async Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var totalServers = await context.GameServers
                .AsNoTracking()
                .CountAsync(gs => !gs.Deleted && gs.AgentEnabled, cancellationToken).ConfigureAwait(false);

            var liveStatuses = await liveStatusStore.GetAllServerLiveStatusesAsync(cancellationToken).ConfigureAwait(false);
            var onlineCount = liveStatuses.Count(s => s.IsOnline);
            var totalPlayers = liveStatuses.Where(s => s.IsOnline).Sum(s => s.CurrentPlayers);

            var unclaimedBanCount = await context.AdminActions
                .AsNoTracking()
                .CountAsync(a => a.Type == (int)AdminActionType.Ban && a.UserProfileId == null, cancellationToken).ConfigureAwait(false);

            var openReportCount = await context.Reports
                .AsNoTracking()
                .CountAsync(r => !r.Closed, cancellationToken).ConfigureAwait(false);

            var actions24h = await GetActionCountsSinceAsync(oneDayAgo, cancellationToken).ConfigureAwait(false);
            var actions7d = await GetActionCountsSinceAsync(sevenDaysAgo, cancellationToken).ConfigureAwait(false);

            var dto = new DashboardSummaryDto
            {
                TotalServers = totalServers,
                OnlineServerCount = onlineCount,
                OfflineServerCount = totalServers - onlineCount,
                TotalPlayersOnline = totalPlayers,
                UnclaimedBanCount = unclaimedBanCount,
                OpenReportCount = openReportCount,
                RecentActions24h = actions24h,
                RecentActions7d = actions7d
            };

            return new ApiResponse<DashboardSummaryDto>(dto).ToApiResult();
        }

        public async Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboardAsync(int days, CancellationToken cancellationToken)
        {
            if (days <= 0)
            {
                days = 30;
            }

            var cutoff = DateTime.UtcNow.AddDays(-days);

            var leaderboard = await context.AdminActions
                .AsNoTracking()
                .Where(a => a.Created >= cutoff && a.UserProfileId != null)
                .GroupBy(a => a.UserProfileId!.Value)
                .Select(g => new
                {
                    AdminId = g.Key,
                    Bans = g.Count(a => a.Type == (int)AdminActionType.Ban),
                    TempBans = g.Count(a => a.Type == (int)AdminActionType.TempBan),
                    Kicks = g.Count(a => a.Type == (int)AdminActionType.Kick),
                    Warnings = g.Count(a => a.Type == (int)AdminActionType.Warning),
                    Observations = g.Count(a => a.Type == (int)AdminActionType.Observation),
                    Total = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var adminIds = leaderboard.Select(x => x.AdminId).ToList();
            var adminProfiles = await context.UserProfiles
                .AsNoTracking()
                .Where(up => adminIds.Contains(up.UserProfileId))
                .Select(up => new { up.UserProfileId, up.DisplayName })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var profileLookup = adminProfiles.ToDictionary(x => x.UserProfileId, x => x.DisplayName ?? "Unknown");

            var entries = leaderboard.Select(x => new AdminLeaderboardEntryDto
            {
                AdminId = x.AdminId,
                DisplayName = profileLookup.GetValueOrDefault(x.AdminId, "Unknown"),
                Bans = x.Bans,
                TempBans = x.TempBans,
                Kicks = x.Kicks,
                Warnings = x.Warnings,
                Observations = x.Observations,
                Total = x.Total
            }).ToList();

            var result = new CollectionModel<AdminLeaderboardEntryDto> { Items = entries };

            return new ApiResponse<CollectionModel<AdminLeaderboardEntryDto>>(result)
            {
                Pagination = new ApiPagination(entries.Count, entries.Count, 0, entries.Count)
            }.ToApiResult();
        }

        public async Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrendAsync(int days, CancellationToken cancellationToken)
        {
            if (days <= 0)
            {
                days = 30;
            }

            var cutoff = DateTime.UtcNow.AddDays(-days);

            var dailyCounts = await context.AdminActions
                .AsNoTracking()
                .Where(a => a.Created >= cutoff)
                .GroupBy(a => a.Created.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Bans = g.Count(a => a.Type == (int)AdminActionType.Ban),
                    TempBans = g.Count(a => a.Type == (int)AdminActionType.TempBan),
                    Kicks = g.Count(a => a.Type == (int)AdminActionType.Kick),
                    Warnings = g.Count(a => a.Type == (int)AdminActionType.Warning),
                    Observations = g.Count(a => a.Type == (int)AdminActionType.Observation)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = dailyCounts.Select(x => new ModerationTrendDataPointDto
            {
                Date = x.Date,
                Bans = x.Bans,
                TempBans = x.TempBans,
                Kicks = x.Kicks,
                Warnings = x.Warnings,
                Observations = x.Observations
            }).ToList();

            var result = new CollectionModel<ModerationTrendDataPointDto> { Items = entries };

            return new ApiResponse<CollectionModel<ModerationTrendDataPointDto>>(result)
            {
                Pagination = new ApiPagination(entries.Count, entries.Count, 0, entries.Count)
            }.ToApiResult();
        }

        public async Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilizationAsync(CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var servers = await context.GameServers
                .AsNoTracking()
                .Where(gs => !gs.Deleted && gs.AgentEnabled)
                .Select(gs => new
                {
                    gs.GameServerId,
                    gs.Title,
                    gs.GameType,
                })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var liveStatuses = await liveStatusStore.GetAllServerLiveStatusesAsync(cancellationToken).ConfigureAwait(false);
            var liveStatusLookup = liveStatuses.ToDictionary(s => s.ServerId);

            var statsAggregates = await context.GameServerStats
                .AsNoTracking()
                .Where(s => s.Timestamp >= cutoff && s.GameServerId != null)
                .GroupBy(s => s.GameServerId!.Value)
                .Select(g => new
                {
                    ServerId = g.Key,
                    AvgPlayers = g.Average(s => (double)s.PlayerCount),
                    PeakPlayers = g.Max(s => s.PlayerCount)
                })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var statsLookup = statsAggregates.ToDictionary(x => x.ServerId);

            var serverDtos = servers.Select(gs =>
            {
                var hasStats = statsLookup.TryGetValue(gs.GameServerId, out var stats);
                var hasLiveStatus = liveStatusLookup.TryGetValue(gs.GameServerId, out var liveStatus);
                var maxPlayers = hasLiveStatus ? liveStatus!.MaxPlayers : 0;
                var avg = hasStats ? stats!.AvgPlayers : 0;

                return new ServerUtilizationDto
                {
                    ServerId = gs.GameServerId,
                    Title = hasLiveStatus && !string.IsNullOrWhiteSpace(liveStatus!.Title) ? liveStatus.Title : gs.Title,
                    GameType = gs.GameType.ToGameType().ToString(),
                    AvgPlayers = Math.Round(avg, 1),
                    PeakPlayers = hasStats ? stats!.PeakPlayers : 0,
                    MaxPlayers = maxPlayers,
                    Utilization = maxPlayers > 0 ? Math.Round(avg / maxPlayers, 3) : 0
                };
            }).ToList();

            var dto = new ServerUtilizationCollectionDto
            {
                Servers = serverDtos,
                TotalAvgPlayers = Math.Round(serverDtos.Sum(s => s.AvgPlayers), 1),
                TotalPeakPlayers = serverDtos.Sum(s => s.PeakPlayers)
            };

            return new ApiResponse<ServerUtilizationCollectionDto>(dto).ToApiResult();
        }

        private async Task<AdminActionCountsDto> GetActionCountsSinceAsync(DateTime since, CancellationToken cancellationToken)
        {
            var counts = await context.AdminActions
                .AsNoTracking()
                .Where(a => a.Created >= since)
                .GroupBy(a => a.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return new AdminActionCountsDto
            {
                Bans = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Ban)?.Count ?? 0,
                TempBans = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.TempBan)?.Count ?? 0,
                Kicks = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Kick)?.Count ?? 0,
                Warnings = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Warning)?.Count ?? 0,
                Observations = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Observation)?.Count ?? 0
            };
        }
    }
}
