namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal static class GameServerSecretId
{
    public static bool IsValid(string? secretId) =>
        !string.IsNullOrWhiteSpace(secretId)
        && secretId.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
}
