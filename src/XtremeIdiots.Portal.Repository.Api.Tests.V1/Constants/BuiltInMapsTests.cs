using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Constants;

public class BuiltInMapsTests
{
    [Theory]
    [InlineData(GameType.CallOfDuty4)]
    [InlineData(GameType.CallOfDuty4x)]
    public void Cod4VariantsShareStockMapsWithoutTreatingCustomOrCod5MapsAsBuiltIn(GameType gameType)
    {
        var stockMaps = BuiltInMaps.GetBuiltInMaps(gameType);

        Assert.Contains("mp_bog", stockMaps);
        Assert.True(stockMaps.SetEquals(BuiltInMaps.GetBuiltInMaps(GameType.CallOfDuty4)));
        foreach (var mapName in stockMaps)
        {
            Assert.True(BuiltInMaps.IsBuiltIn(gameType, mapName));
            Assert.True(BuiltInMaps.IsBuiltIn(gameType, mapName.ToUpperInvariant()));
        }
        Assert.False(BuiltInMaps.IsBuiltIn(gameType, "mp_custom"));
        Assert.False(BuiltInMaps.IsBuiltIn(gameType, "mp_airfield"));
        Assert.False(BuiltInMaps.IsBuiltIn(GameType.CallOfDuty5, "mp_bog"));
    }

    [Fact]
    public void UnsupportedGamesDoNotInheritCod4StockMaps()
    {
        Assert.Empty(BuiltInMaps.GetBuiltInMaps(GameType.CallOfDuty2));
        Assert.False(BuiltInMaps.IsBuiltIn(GameType.CallOfDuty2, "mp_bog"));
        Assert.True(BuiltInMaps.IsBuiltIn(GameType.CallOfDuty5, "mp_airfield"));
    }
}
