namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

public sealed record SettingsValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = [];
}
