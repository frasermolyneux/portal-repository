using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class ConnectedPlayerTagProjection
{
    private static readonly string[] Names =
        ["verified-player", "senior-admin", "head-admin", "game-admin", "moderator", "clan-member"];

    public static async Task ReconcileAsync(PortalDbContext context, Guid playerId, CancellationToken cancellationToken)
    {
        var tags = await context.Tags.Where(t => Names.Contains(t.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (tags.Count != Names.Length || tags.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Names.Length)
        {
            throw new InvalidOperationException("The connected-player system tags are missing or duplicated.");
        }

        var ownership = await context.ConnectedPlayerProfiles.AsNoTracking()
            .Where(p => p.PlayerId == playerId && p.IsActive && p.UserProfile.ArchivedAtUtc == null)
            .Select(p => new { p.UserProfileId, p.Player.GameType })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ownership is not null)
        {
            desired.Add("verified-player");
            var claims = await context.UserProfileClaims.AsNoTracking()
                .Where(c => c.UserProfileId == ownership.UserProfileId && c.SystemGenerated)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var game = ((GameType)ownership.GameType).ToString();
            if (claims.Any(c => c.ClaimType == UserProfileClaimType.Webmaster || c.ClaimType == UserProfileClaimType.SeniorAdmin))
            {
                desired.Add("senior-admin");
            }

            if (claims.Any(c => c.ClaimType == UserProfileClaimType.HeadAdmin && string.Equals(c.ClaimValue, game, StringComparison.OrdinalIgnoreCase)))
            {
                desired.Add("head-admin");
            }

            if (claims.Any(c => c.ClaimType == UserProfileClaimType.GameAdmin && string.Equals(c.ClaimValue, game, StringComparison.OrdinalIgnoreCase)))
            {
                desired.Add("game-admin");
            }

            if (claims.Any(c => c.ClaimType == UserProfileClaimType.Moderator && string.Equals(c.ClaimValue, game, StringComparison.OrdinalIgnoreCase)))
            {
                desired.Add("moderator");
            }

            if (claims.Any(c => c.ClaimType == UserProfileClaimType.ClanMember))
            {
                desired.Add("clan-member");
            }
        }

        var tagIds = tags.Select(t => t.TagId).ToArray();
        var existing = await context.PlayerTags.Where(t => t.PlayerId == playerId && t.TagId != null && tagIds.Contains(t.TagId.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var tag in tags)
        {
            var matches = existing.Where(t => t.TagId == tag.TagId).OrderBy(t => t.Assigned).ThenBy(t => t.PlayerTagId).ToList();
            if (!desired.Contains(tag.Name))
            {
                context.PlayerTags.RemoveRange(matches);
            }
            else if (matches.Count == 0)
            {
                context.PlayerTags.Add(new PlayerTag { PlayerTagId = Guid.NewGuid(), PlayerId = playerId, TagId = tag.TagId, Assigned = DateTime.UtcNow });
            }
            else if (matches.Count > 1)
            {
                context.PlayerTags.RemoveRange(matches.Skip(1));
            }
        }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
