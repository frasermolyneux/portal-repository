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
        const string configuration = """
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
        Assert.Equal("@Portal.KeyVault(SecretId=sftp-password)", document["password"]?.Value<string>());
        Assert.Equal("@Portal.KeyVault(SecretId=sftp-private-key)", document["privateKey"]?.Value<string>());
        Assert.Equal("@Portal.KeyVault(SecretId=sftp-private-key-passphrase)", document["privateKeyPassphrase"]?.Value<string>());
    }

    [Fact]
    public async Task ResolveSecretsAsync_ReferencedSecrets_ReturnsPlaintextShape()
    {
        var gameServerId = Guid.NewGuid();
        var store = new InMemoryGameServerSecretStore();
        store.Seed(gameServerId, "sftp-private-key", "private-key-content");
        store.Seed(gameServerId, "sftp-private-key-passphrase", "key-passphrase");
        var subject = new GameServerConfigurationSecretProtector(store);
        const string configuration = """
            {
                "privateKey": "@Portal.KeyVault(SecretId=sftp-private-key)",
                "privateKeyPassphrase": "@Portal.KeyVault(SecretId=sftp-private-key-passphrase)"
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
    }

    [Fact]
    public async Task ResolveSecretsAsync_LegacyPlaintext_ReturnsOriginalConfiguration()
    {
        var subject = new GameServerConfigurationSecretProtector(new InMemoryGameServerSecretStore());
        const string configuration = """{"password":"legacy-password"}""";

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
        const string configuration = """{"password":"@Portal.KeyVault(SecretId=ftp-password)"}""";

        var result = await subject.ExternalizeSecretsAsync(
            Guid.NewGuid(),
            "ftp",
            configuration,
            CancellationToken.None);

        Assert.Same(configuration, result);
        Assert.Equal(0, store.SetCallCount);
    }

    [Theory]
    [InlineData("@Portal.KeyVault(SecretId=other-secret)")]
    [InlineData("@Portal.KeyVault(BadReference)")]
    public async Task ExternalizeSecretsAsync_InvalidReference_Throws(string reference)
    {
        var subject = new GameServerConfigurationSecretProtector(new InMemoryGameServerSecretStore());
        var configuration = $$"""{"password":"{{reference}}"}""";

        await Assert.ThrowsAsync<InvalidGameServerCredentialReferenceException>(() =>
            subject.ExternalizeSecretsAsync(
                Guid.NewGuid(),
                "ftp",
                configuration,
                CancellationToken.None));
    }

    private sealed class InMemoryGameServerSecretStore : IGameServerSecretStore
    {
        private readonly Dictionary<(Guid GameServerId, string SecretId), string> secrets = [];

        public int SetCallCount { get; private set; }

        public Task<string> GetSecretAsync(Guid gameServerId, string secretId, CancellationToken cancellationToken) =>
            Task.FromResult(secrets[(gameServerId, secretId)]);

        public Task SetSecretAsync(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken)
        {
            SetCallCount++;
            secrets[(gameServerId, secretId)] = secretValue;
            return Task.CompletedTask;
        }

        public void Seed(Guid gameServerId, string secretId, string value) =>
            secrets[(gameServerId, secretId)] = value;

        public string Get(Guid gameServerId, string secretId) => secrets[(gameServerId, secretId)];
    }
}
