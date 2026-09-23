namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal static class GameServerSecretId
{
    private const int MaximumLength = 90;

    public static bool IsValid(string? secretId) =>
        !string.IsNullOrWhiteSpace(secretId)
        && secretId.Length <= MaximumLength
        && secretId.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
}
