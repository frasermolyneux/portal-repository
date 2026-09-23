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
                new KeyVaultSecret(secretName, "unchanged"),
                Mock.Of<Response>()));
        var subject = new KeyVaultGameServerSecretStore(client.Object);

        await subject.SetSecretAsync(gameServerId, "ftp-password", "unchanged", CancellationToken.None);

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
                new KeyVaultSecret(secretName, "old-value"),
                Mock.Of<Response>()));
        client
            .Setup(instance => instance.SetSecretAsync(secretName, "new-value", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                new KeyVaultSecret(secretName, "new-value"),
                Mock.Of<Response>()));
        var subject = new KeyVaultGameServerSecretStore(client.Object);

        await subject.SetSecretAsync(gameServerId, "sftp-password", "new-value", CancellationToken.None);

        client.Verify(
            instance => instance.SetSecretAsync(secretName, "new-value", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
