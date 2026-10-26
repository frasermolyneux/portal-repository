namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

internal static class UserProfileIdentifierComparison
{
    // Match the database's case-insensitive, space-padded identifier equality in
    // in-memory batch checks and consumer fakes.
    public static bool Equals(string? left, string? right)
        => left != null && right != null && StringComparer.OrdinalIgnoreCase.Equals(left.TrimEnd(' '), right.TrimEnd(' '));

    public static bool HasDuplicates(IEnumerable<string?> values)
        => values.Where(value => value != null)
            .GroupBy(value => value!.TrimEnd(' '), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);
}
