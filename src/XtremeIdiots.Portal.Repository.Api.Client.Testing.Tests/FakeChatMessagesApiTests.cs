using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public sealed class FakeChatMessagesApiTests
{
    [Theory]
    [InlineData(" Al ", "Alice,Bob")]
    [InlineData(" Ali ", "Alice")]
    [InlineData(" hello ", "Alice,Sally")]
    public async Task History_SearchesUsernamesAndMessagesWithTrimmedShortPrefixes(string term, string expected)
    {
        var fake = new FakeChatMessagesApi()
            .AddChatMessage(RepositoryDtoFactory.CreateChatMessage(username: "Alice", message: "hello"))
            .AddChatMessage(RepositoryDtoFactory.CreateChatMessage(username: "Bob", message: "Albatross spotted"))
            .AddChatMessage(RepositoryDtoFactory.CreateChatMessage(username: "Sally", message: "hello Al"));

        var result = await fake.GetChatMessagesPage(filterString: term);

        Assert.Equal(expected.Split(','), result.Result!.Data!.Items.Select(x => x.Username).Order());
    }
}
