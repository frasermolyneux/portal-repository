namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Sort order options for the <c>GetConnectedPlayers</c> query. Matches the columns
    /// exposed by the portal-web ConnectedPlayers DataTables view. Default (no value)
    /// resolves to <see cref="LinkedAtUtcDesc"/>.
    /// </summary>
    public enum ConnectedPlayersOrder
    {
        GameTypeAsc,
        GameTypeDesc,
        UsernameAsc,
        UsernameDesc,
        LinkMethodAsc,
        LinkMethodDesc,
        IsActiveAsc,
        IsActiveDesc,
        LinkedAtUtcAsc,
        LinkedAtUtcDesc,
        UnlinkedAtUtcAsc,
        UnlinkedAtUtcDesc
    }
}
