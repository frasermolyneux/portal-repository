using System.Net;
using System.Net.Http.Json;
using MX.Api.Abstractions;
using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class UserProfileArchivalTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Archive_HttpContractPreservesHistoryAndExcludesArchivedProfiles()
    {
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        factory.SeedDatabase(context =>
        {
            context.UserProfiles.AddRange(
                new UserProfile { UserProfileId = targetId, IdentityOid = "archival-identity", XtremeIdiotsForumId = "archival-forum", DemoAuthKey = "demo" },
                new UserProfile { UserProfileId = actorId });
            context.SaveChanges();
        });
        using var client = factory.CreateClient();
        var dto = new ArchiveUserProfileDto(targetId, actorId, "Consistent upstream 404");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/v1/user-profile/{actorId}/archive", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/v1/user-profile/{actorId}/archive", new ArchiveUserProfileDto(actorId, actorId, "Self"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/v1/user-profile/{targetId}/archive",
            new { userProfileId = targetId, archivedByUserProfileId = actorId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/v1/user-profile/{targetId}/archive",
            new { userProfileId = targetId, archivedByUserProfileId = actorId, reason = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/v1/user-profile/{targetId}/archive", new ArchiveUserProfileDto(targetId, actorId, "  "))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/v1/user-profile/{targetId}/archive", new ArchiveUserProfileDto(targetId, actorId, new string('x', 501)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/v1/user-profile/{targetId}/archive", dto)).StatusCode);
        var repeated = await client.PostAsJsonAsync($"/v1/user-profile/{targetId}/archive", new ArchiveUserProfileDto(targetId, actorId, "Changed"));
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        Assert.Empty(await repeated.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/v1/user-profile/{targetId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/v1/user-profile/by-identity-id/archival-identity")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/v1/user-profile/by-xtremeidiots-id/archival-forum")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/v1/user-profile/by-demo-auth-key/demo")).StatusCode);
        var historicalResponse = await client.GetAsync($"/v1/user-profile/{targetId}/including-archived");
        Assert.Equal(HttpStatusCode.OK, historicalResponse.StatusCode);
        var historical = JsonConvert.DeserializeObject<ApiResponse<UserProfileDto>>(await historicalResponse.Content.ReadAsStringAsync())!.Data!;
        Assert.NotNull(historical.ArchivedAtUtc);
        Assert.Equal(actorId, historical.ArchivedByUserProfileId);
        Assert.Equal(dto.Reason, historical.ArchiveReason);
        var all = await client.GetAsync("/v1/user-profiles");
        var profiles = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<UserProfileDto>>>(await all.Content.ReadAsStringAsync())!;
        Assert.Single(profiles.Data!.Items!);
        Assert.Equal(1, profiles.Pagination!.TotalCount);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/user-profile", new CreateUserProfileDto("archival-forum", "Login", "test@example.test") { IdentityOid = "new-identity" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/v1/user-profile/{targetId}/claims", Array.Empty<CreateUserProfileClaimDto>())).StatusCode);
    }
}
