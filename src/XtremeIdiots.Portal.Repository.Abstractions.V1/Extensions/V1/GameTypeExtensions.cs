using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1
{
    public static class GameTypeExtensions
    {
        public static GameType ToGameType(this string gameType)
        {
            return Enum.Parse<GameType>(gameType);
        }

        public static string DisplayName(this GameType gameType)
        {
            return gameType switch
            {
                GameType.CallOfDuty2 => "Call of Duty 2",
                GameType.CallOfDuty4 => "Call of Duty 4",
                GameType.CallOfDuty5 => "Call of Duty 5",
                _ => gameType.ToString()
            };
        }

        public static string ShortDisplayName(this GameType gameType)
        {
            return gameType switch
            {
                GameType.CallOfDuty2 => "COD2",
                GameType.CallOfDuty4 => "COD4",
                GameType.CallOfDuty5 => "COD5",
                _ => gameType.ToString()
            };
        }
    }
}
