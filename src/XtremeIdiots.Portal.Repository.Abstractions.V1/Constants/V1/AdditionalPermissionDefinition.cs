namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Defines metadata for an assignable additional permission.
    /// </summary>
    /// <param name="ClaimType">The claim type string stored in the database (matches the policy name).</param>
    /// <param name="DisplayName">Human-readable label for the permission picker UI.</param>
    /// <param name="Description">Short description of what this permission grants.</param>
    /// <param name="Domain">The top-level domain grouping for UI organisation (e.g., "Map Rotations", "Game Servers").</param>
    /// <param name="SubDomain">Optional sub-domain grouping within the domain (e.g., "Credentials", "RCON"). Null for top-level permissions.</param>
    /// <param name="Scope">What value types this permission accepts.</param>
    public record AdditionalPermissionDefinition(
        string ClaimType,
        string DisplayName,
        string Description,
        string Domain,
        string? SubDomain,
        PermissionScope Scope);
}
