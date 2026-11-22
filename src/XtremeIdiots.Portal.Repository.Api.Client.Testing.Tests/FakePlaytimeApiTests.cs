using System.Net;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

public class FakePlaytimeApiTests
{
    [Fact]
    public async Task CurrentSessions_AreConfiguredPerServerAndReset()
    {
        var fake = new FakePlaytimeApi();
        var server = Guid.NewGuid();
        var player = Guid.NewGuid();
        var request = new CurrentPlayerSessionsRequestDto { PlayerIds = [player] };
        var configured = new CurrentPlayerSessionResultDto
        {
            PlayerId = player,
            Status = CurrentPlayerSessionStatus.Found,
            Session = new CurrentPlayerSessionDto { ServerId = server, ObservedSeconds = 120 }
        };
        fake.SetCurrentSession(server, configured);
        Assert.Equal(configured, Assert.Single((await fake.GetCurrentSessions(server, request)).Result!.Data!));
        Assert.Equal(CurrentPlayerSessionStatus.Unavailable,
            Assert.Single((await fake.GetCurrentSessions(Guid.NewGuid(), request)).Result!.Data!).Status);
        fake.Reset();
        Assert.Equal(CurrentPlayerSessionStatus.Unavailable,
            Assert.Single((await fake.GetCurrentSessions(server, request)).Result!.Data!).Status);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await fake.GetCurrentSessions(server, new CurrentPlayerSessionsRequestDto { PlayerIds = [player, player] })).StatusCode);
    }
}
