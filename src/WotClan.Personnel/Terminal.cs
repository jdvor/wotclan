namespace WotClan.Personnel;

using Spectre.Console;

internal static class Terminal
{
    public static void Write(PersonnelResponse pr)
    {
        var table = new Table();
        table.AddColumns("name", "account", "last played", "battles", "win (%)", "exp/battle");
        table.Columns[3].Alignment = Justify.Right;
        table.Columns[4].Alignment = Justify.Right;
        table.Columns[5].Alignment = Justify.Right;

        foreach (var p in pr.Players.Where(x => x.AccountName != "AutoInfo").OrderBy(x => x.BattlesCount))
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(p.LastBattleTime);
            table.AddRow(
                p.AccountName,
                p.AccountId.ToString(),
                dt.ToString("u"),
                p.BattlesCount.ToString(),
                p.WinsPercentage.ToString("F1"),
                p.ExperiencePerBattle.ToString("F1"));
        }

        AnsiConsole.Write(table);
    }

    public static void Write(Exception ex, string msg)
    {
        Console.Error.WriteLine(msg);
        Console.Error.WriteLine(ex);
    }
}
