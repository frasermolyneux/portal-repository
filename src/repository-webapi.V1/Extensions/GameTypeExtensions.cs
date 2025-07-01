using XtremeIdiots.CallOfDuty.DemoReader.Models;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.RepositoryWebApi.V1.Extensions
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
            switch (portalGameType)
            {
                case GameType.CallOfDuty2:
                    return GameVersion.CallOfDuty2;
                case GameType.CallOfDuty4:
                    return GameVersion.CallOfDuty4;
                case GameType.CallOfDuty5:
                    return GameVersion.CallOfDuty5;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gameType));
            }
        }

        public static int ToGameTypeInt(this GameType gameType)
        {
            return (int)gameType;
        }

        public static string DemoExtension(this GameType gameType)
        {
            switch (gameType)
            {
                case GameType.CallOfDuty2:
                    return "dm_1";
                case GameType.CallOfDuty4:
                    return "dm_1";
                case GameType.CallOfDuty5:
                    return "dm_6";
                default:
                    throw new ApplicationException("Game Type not supported for demos");
            }
        }
    }
}
