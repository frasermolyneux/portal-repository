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
        string[] expectedStockMaps =
        [
            "mp_backlot", "mp_bloc", "mp_bog", "mp_broadcast", "mp_carentan",
            "mp_cargoship", "mp_citystreets", "mp_convoy", "mp_countdown",
            "mp_crash", "mp_crash_snow", "mp_creek", "mp_crossfire", "mp_farm",
            "mp_killhouse", "mp_overgrown", "mp_pipeline", "mp_shipment",
            "mp_showdown", "mp_strike", "mp_vacant"
        ];
        var stockMaps = BuiltInMaps.GetBuiltInMaps(gameType);

        Assert.True(stockMaps.SetEquals(expectedStockMaps),
            $"Unexpected stock maps: {string.Join(", ", stockMaps.Except(expectedStockMaps))}; " +
            $"missing stock maps: {string.Join(", ", expectedStockMaps.Except(stockMaps))}");
        foreach (var mapName in expectedStockMaps)
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
