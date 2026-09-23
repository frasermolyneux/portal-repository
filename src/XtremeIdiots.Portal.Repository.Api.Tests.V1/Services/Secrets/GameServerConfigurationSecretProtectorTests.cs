using Newtonsoft.Json.Linq;

using Xunit;

using XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services.Secrets;

public sealed class GameServerConfigurationSecretProtectorTests
{
    [Fact]
    public async Task ExternalizeSecretsAsync_SftpConfiguration_StoresEachPopulatedSecret()
    {
        var gameServerId = Guid.NewGuid();
        var store = new InMemoryGameServerSecretStore();
        var subject = new GameServerConfigurationSecretProtector(store);
        const string configuration = /*lang=json,strict*/ """
            {
                "authenticationType": "PrivateKey",
                "password": "fallback-password",
                "privateKey": "private-key-content",
                "privateKeyPassphrase": "key-passphrase"
            }
            """;

        var result = await subject.ExternalizeSecretsAsync(
            gameServerId,
            "sftp",
            configuration,
            CancellationToken.None);
        var document = JObject.Parse(result);

        Assert.Equal("fallback-password", store.Get(gameServerId, "sftp-password"));
        Assert.Equal("private-key-content", store.Get(gameServerId, "sftp-private-key"));
        Assert.Equal("key-passphrase", store.Get(gameServerId, "sftp-private-key-passphrase"));
        Assert.Equal("@Portal.KeyVault(SecretId=sftp-password;Version=testversion)", document["password"]?.Value<string>());
        Assert.Equal("@Portal.KeyVault(SecretId=sftp-private-key;Version=testversion)", document["privateKey"]?.Value<string>());
        Assert.Equal("@Portal.KeyVault(SecretId=sftp-private-key-passphrase;Version=testversion)", document["privateKeyPassphrase"]?.Value<string>());
    }

    [Fact]
    public async Task ResolveSecretsAsync_ReferencedSecrets_ReturnsPlaintextShape()
    {
        var gameServerId = Guid.NewGuid();
        var store = new InMemoryGameServerSecretStore();
        store.Seed(gameServerId, "sftp-private-key", "private-key-content");
        store.Seed(gameServerId, "sftp-private-key-passphrase", "key-passphrase");
        var subject = new GameServerConfigurationSecretProtector(store);
        const string configuration = /*lang=json,strict*/ """
            {
                "privateKey": "@Portal.KeyVault(SecretId=sftp-private-key;Version=privatekeyversion)",
                "privateKeyPassphrase": "@Portal.KeyVault(SecretId=sftp-private-key-passphrase;Version=passphraseversion)"
            }
            """;

        var result = await subject.ResolveSecretsAsync(
            gameServerId,
            "sftp",
            configuration,
            CancellationToken.None);
        var document = JObject.Parse(result);

        Assert.Equal("private-key-content", document["privateKey"]?.Value<string>());
        Assert.Equal("key-passphrase", document["privateKeyPassphrase"]?.Value<string>());
        Assert.Equal("privatekeyversion", store.GetRequestedVersion("sftp-private-key"));
        Assert.Equal("passphraseversion", store.GetRequestedVersion("sftp-private-key-passphrase"));
    }

    [Fact]
    public async Task ResolveSecretsAsync_LegacyPlaintext_ReturnsOriginalConfiguration()
    {
        var subject = new GameServerConfigurationSecretProtector(new InMemoryGameServerSecretStore());
        const string configuration = /*lang=json,strict*/ """{"password":"legacy-password"}""";

        var result = await subject.ResolveSecretsAsync(
            Guid.NewGuid(),
            "ftp",
            configuration,
            CancellationToken.None);

        Assert.Same(configuration, result);
    }

    [Fact]
    public async Task ExternalizeSecretsAsync_ExistingReference_DoesNotWriteSecretAgain()
    {
        var store = new InMemoryGameServerSecretStore();
        var subject = new GameServerConfigurationSecretProtector(store);
        const string configuration = /*lang=json,strict*/ """{"password":"@Portal.KeyVault(SecretId=ftp-password;Version=testversion)"}""";

        var result = await subject.ExternalizeSecretsAsync(
            Guid.NewGuid(),
            "ftp",
            configuration,
            CancellationToken.None);

        Assert.Same(configuration, result);
        Assert.Equal(0, store.SetCallCount);
    }

    [Theory]
    [InlineData("@Portal.KeyVault(SecretId=other-secret;Version=testversion)")]
    [InlineData("@Portal.KeyVault(BadReference)")]
    [InlineData("@Portal.KeyVault(SecretId=ftp-password;Version=abc;Version=def)")]
    [InlineData("@Portal.KeyVault(SecretId=ftp-password;Version=abc def)")]
    [InlineData("@Portal.KeyVault(SecretId=ftp-password;Version=abc))")]
    public async Task ExternalizeSecretsAsync_InvalidReference_Throws(string reference)
    {
        var store = new InMemoryGameServerSecretStore();
        var subject = new GameServerConfigurationSecretProtector(store);
        var configuration = $$"""{"password":"{{reference}}"}""";

        await Assert.ThrowsAsync<InvalidGameServerCredentialReferenceException>(() =>
            subject.ExternalizeSecretsAsync(
                Guid.NewGuid(),
                "ftp",
                configuration,
                CancellationToken.None));
        Assert.Equal(0, store.SetCallCount);
    }

    [Fact]
    public async Task ExternalizeSecretsAsync_InvalidLaterField_DoesNotWriteEarlierSecret()
    {
        var store = new InMemoryGameServerSecretStore();
        var subject = new GameServerConfigurationSecretProtector(store);
        const string configuration = /*lang=json,strict*/ """
            {
                "password": "password",
                "privateKey": "@Portal.KeyVault(SecretId=unexpected-secret;Version=testversion)"
            }
            """;

        await Assert.ThrowsAsync<InvalidGameServerCredentialReferenceException>(() =>
            subject.ExternalizeSecretsAsync(
                Guid.NewGuid(),
                "sftp",
                configuration,
                CancellationToken.None));
        Assert.Equal(0, store.SetCallCount);
    }

    [Fact]
    public async Task ExternalizeSecretsAsync_CaseVariantDuplicateCredential_Throws()
    {
        var store = new InMemoryGameServerSecretStore();
        var subject = new GameServerConfigurationSecretProtector(store);
        const string configuration = /*lang=json,strict*/ """
            {
                "password": "first-password",
                "Password": "second-password"
            }
            """;

        await Assert.ThrowsAsync<InvalidGameServerCredentialReferenceException>(() =>
            subject.ExternalizeSecretsAsync(
                Guid.NewGuid(),
                "ftp",
                configuration,
                CancellationToken.None));
        Assert.Equal(0, store.SetCallCount);
    }

    private sealed class InMemoryGameServerSecretStore : IGameServerSecretStore
    {
        private readonly Dictionary<(Guid GameServerId, string SecretId), string> secrets = [];
        private readonly Dictionary<string, string?> requestedVersions = [];

        public int SetCallCount { get; private set; }

        public Task<string> GetSecretAsync(
            Guid gameServerId,
            string secretId,
            string? secretVersion,
            CancellationToken cancellationToken)
        {
            requestedVersions[secretId] = secretVersion;
            return Task.FromResult(secrets[(gameServerId, secretId)]);
        }

        public Task<string> SetSecretAsync(
            Guid gameServerId,
            string secretId,
            string secretValue,
            CancellationToken cancellationToken)
        {
            SetCallCount++;
            secrets[(gameServerId, secretId)] = secretValue;
            return Task.FromResult("testversion");
        }

        public void Seed(Guid gameServerId, string secretId, string value) =>
            secrets[(gameServerId, secretId)] = value;

        public string Get(Guid gameServerId, string secretId) => secrets[(gameServerId, secretId)];

        public string? GetRequestedVersion(string secretId) => requestedVersions[secretId];
    }
}
