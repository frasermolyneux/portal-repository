namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

/// <summary>
/// Defines maps included in CoD4 and CoD5 dedicated server installations
/// (base game, DLC, and bonus content). These maps are already present on
/// servers and do not require FTP file uploads during map rotation sync.
/// </summary>
public static class BuiltInMaps
{
    private static readonly IReadOnlyDictionary<GameType, IReadOnlySet<string>> Maps =
        new Dictionary<GameType, IReadOnlySet<string>>
        {
            [GameType.CallOfDuty4] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "mp_ambush",
                "mp_backlot",
                "mp_bloc",
                "mp_bog",
                "mp_broadcast",
                "mp_cargoship",
                "mp_chinatown",
                "mp_countdown",
                "mp_crash",
                "mp_creek",
                "mp_crossfire",
                "mp_district",
                "mp_downpour",
                "mp_killhouse",
                "mp_overgrown",
                "mp_pipeline",
                "mp_shipment",
                "mp_showdown",
                "mp_strike",
                "mp_vacant"
            },
            [GameType.CallOfDuty5] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "mp_airfield",
                "mp_asylum",
                "mp_banzai",
                "mp_bgate",
                "mp_castle",
                "mp_cliffside",
                "mp_corrosion",
                "mp_courtyard",
                "mp_docks",
                "mp_dome",
                "mp_downfall",
                "mp_drum",
                "mp_hangar",
                "mp_kneedeep",
                "mp_kwai",
                "mp_makin",
                "mp_makin_day",
                "mp_nachtfeuer",
                "mp_outskirts",
                "mp_roundhouse",
                "mp_seelow",
                "mp_shrine",
                "mp_stalingrad",
                "mp_station",
                "mp_suburban",
                "mp_subway",
                "mp_upheaval",
                "mp_vodka"
            }
        };

    /// <summary>
    /// Returns true if the map is a stock/built-in map for the given game type.
    /// </summary>
    public static bool IsBuiltIn(GameType gameType, string mapName)
    {
        return Maps.TryGetValue(gameType, out var maps) && maps.Contains(mapName);
    }

    /// <summary>
    /// Returns the set of built-in map names for a game type, or an empty set if none.
    /// </summary>
    public static IReadOnlySet<string> GetBuiltInMaps(GameType gameType)
    {
        return Maps.TryGetValue(gameType, out var maps) ? maps : new HashSet<string>();
    }
}
