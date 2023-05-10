namespace WotClan.Personnel;

public enum BattleType
{
    Default = 0,
    Random,
    Team,
    Skirmish,
    Advance,
    GlobalMap,
}

internal static class BattleTypeExtensions
{
    internal static string ToHttp(this BattleType bt)
    {
        return bt switch
        {
            BattleType.Random => "random",
            BattleType.Team => "team",
            BattleType.Skirmish => "fort_sorties",
            BattleType.Advance => "fort_battles",
            BattleType.GlobalMap => "global_map",
            _ => "default",
        };
    }
}
