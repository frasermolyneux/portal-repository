using Azure;
using Azure.Security.KeyVault.Secrets;

using Moq;
using Xunit;

using XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services.Secrets;

public sealed class KeyVaultGameServerSecretStoreTests
{
    [Fact]
    public async Task SetSecretAsync_UnchangedValue_DoesNotCreateNewVersion()
    {
        var gameServerId = Guid.NewGuid();
        var secretName = $"{gameServerId:D}-ftp-password";
        var client = new Mock<SecretClient>(MockBehavior.Strict);
        client
            .Setup(instance => instance.GetSecretAsync(
                secretName,
                It.Is<string?>(version => version == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                CreateSecret(secretName, "existing-version", "unchanged"),
                Mock.Of<Response>()));
        var subject = new KeyVaultGameServerSecretStore(client.Object);

        var version = await subject.SetSecretAsync(
            gameServerId,
            "ftp-password",
            "unchanged",
            CancellationToken.None);

        Assert.Equal("existing-version", version);
        client.Verify(
            instance => instance.SetSecretAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetSecretAsync_ChangedValue_CreatesNewVersion()
    {
        var gameServerId = Guid.NewGuid();
        var secretName = $"{gameServerId:D}-sftp-password";
        var client = new Mock<SecretClient>(MockBehavior.Strict);
        client
            .Setup(instance => instance.GetSecretAsync(
                secretName,
                It.Is<string?>(version => version == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                CreateSecret(secretName, "old-version", "old-value"),
                Mock.Of<Response>()));
        client
            .Setup(instance => instance.SetSecretAsync(secretName, "new-value", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                CreateSecret(secretName, "new-version", "new-value"),
                Mock.Of<Response>()));
        var subject = new KeyVaultGameServerSecretStore(client.Object);

        var version = await subject.SetSecretAsync(
            gameServerId,
            "sftp-password",
            "new-value",
            CancellationToken.None);

        Assert.Equal("new-version", version);
        client.Verify(
            instance => instance.SetSecretAsync(secretName, "new-value", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static KeyVaultSecret CreateSecret(string name, string version, string value)
    {
        var vaultUri = new Uri("https://unit-test.vault.azure.net/");
        var properties = SecretModelFactory.SecretProperties(
            new Uri(vaultUri, $"secrets/{name}/{version}"),
            vaultUri,
            name,
            version,
            false,
            new Uri(vaultUri, "keys/unit-test"),
            null,
            null,
            "Recoverable");
        return SecretModelFactory.KeyVaultSecret(properties, value);
    }
}
