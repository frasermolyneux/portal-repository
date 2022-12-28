using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApiClient;
using XtremeIdiots.Portal.RepositoryApiClient.Api;

namespace repository_webapi.IntegrationTests;

public class PlayersApiTests
{
    IPlayersApi? playersApi;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine($"Using API Base URL: {Environment.GetEnvironmentVariable("api_base_url")}");

        var fakeMemoryCache = A.Fake<IMemoryCache>();
        var fakeRepositoryApiTokenProviderLogger = A.Fake<ILogger<RepositoryApiTokenProvider>>();

        IConfiguration config = A.Fake<IConfiguration>();
        A.CallTo(() => config["repository_api_application_audience"]).Returns(Environment.GetEnvironmentVariable("api_audience"));

        var tokenProvider = new RepositoryApiTokenProvider(fakeRepositoryApiTokenProviderLogger, fakeMemoryCache, config);

        var fakePlayersApiLogger = A.Fake<ILogger<PlayersApi>>();

        var repositoryApiClientOptions = Options.Create<RepositoryApiClientOptions>(new RepositoryApiClientOptions()
        {
            BaseUrl = Environment.GetEnvironmentVariable("api_base_url"),
            ApiKey = Environment.GetEnvironmentVariable("api_key"),
            ApiPathPrefix = null
        });

        playersApi = new PlayersApi(fakePlayersApiLogger, repositoryApiClientOptions, tokenProvider, fakeMemoryCache);
    }

    [Test]
    public async Task Test1()
    {
        // Arrange

        // Act
        var result = await playersApi.HeadPlayerByGameType(GameType.CallOfDuty2, "non-existing-guid");

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

        // Assert
        result.IsNotFound.Should().BeTrue();
    }
}