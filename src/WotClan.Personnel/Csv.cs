using System.Text;

namespace WotClan.Personnel;

internal static class Csv
{
    public static async Task WriteAsync(PersonnelResponse pr, string path)
    {
        const char sep = ',';

        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        using var w = new StreamWriter(path, false, Encoding.UTF8, 16_384);

        await w.WriteLineAsync($"name{sep}account{sep}last_played{sep}battles{sep}win_pct{sep}exp_battle");
        foreach (var p in pr.Players.Where(x => x.AccountName != "AutoInfo").OrderBy(x => x.BattlesCount))
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(p.LastBattleTime);
            await w.WriteAsync($"\"{p.AccountName}\"{sep}{p.AccountId}{sep}\"{dt:u}\"{sep}{p.BattlesCount}{sep}");
            await w.WriteLineAsync($"{p.WinsPercentage:F1}{sep}{p.ExperiencePerBattle:F1}");
        }

        await w.FlushAsync();
        w.Close();
    }
}
