using XtremeIdiots.CallOfDuty.DemoReader.Models;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Extensions
{
    public static class GameTypeExtensions
    {
        public static GameType ToGameType(this int gameType)
        {
            return (GameType)gameType;
        }

        public static GameVersion ToCodDemoReaderGameVersion(this int gameType)
        {
            var portalGameType = (GameType)gameType;
            return portalGameType switch
            {
                GameType.CallOfDuty2 => GameVersion.CallOfDuty2,
                GameType.CallOfDuty4 => GameVersion.CallOfDuty4,
                GameType.CallOfDuty5 => GameVersion.CallOfDuty5,
                _ => throw new ArgumentOutOfRangeException(nameof(gameType))
            };
        }

        public static int ToGameTypeInt(this GameType gameType)
        {
            return (int)gameType;
        }

        public static string DemoExtension(this GameType gameType)
        {
            return gameType switch
            {
                GameType.CallOfDuty2 => "dm_1",
                GameType.CallOfDuty4 => "dm_1",
                GameType.CallOfDuty5 => "dm_6",
                _ => throw new ApplicationException("Game Type not supported for demos")
            };
        }
    }
}
