namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

public static class SchemaVersionSupport
{
    public const int LegacySchemaVersion = 0;
    public const int CurrentSchemaVersion = 1;

    public static IReadOnlySet<int> SupportedSchemaVersions { get; } = new HashSet<int>
    {
        LegacySchemaVersion,
        CurrentSchemaVersion
    };

    public static bool IsSupported(int schemaVersion)
    {
        return SupportedSchemaVersions.Contains(schemaVersion);
    }
}
