namespace WotClan.Personnel;

using OfficeOpenXml;
using OfficeOpenXml.Style;

internal static class Xlsx
{
    public static async Task WriteAsync(PersonnelResponse pr, string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var xlsx = new ExcelPackage();
        var ws = xlsx.Workbook.Worksheets.Add("1");

        ws.Cells[1, 1].Value = "name";
        ws.Cells[1, 2].Value = "account";
        ws.Cells[1, 3].Value = "last played";
        ws.Cells[1, 4].Value = "battles";
        ws.Cells[1, 5].Value = "wins (%)";
        ws.Cells[1, 6].Value = "exp/battle";

        var r = 1;
        foreach (var p in pr.Players.Where(x => x.AccountName != "AutoInfo").OrderBy(x => x.BattlesCount))
        {
            ++r;
            var dt = DateTimeOffset.FromUnixTimeSeconds(p.LastBattleTime);
            ws.Cells[r, 1].Value = p.AccountName;
            ws.Cells[r, 2].Value = p.AccountId;
            ws.Cells[r, 3].Value = dt;
            ws.Cells[r, 4].Value = p.BattlesCount;
            ws.Cells[r, 5].Value = p.WinsPercentage / 100;
            ws.Cells[r, 6].Value = p.ExperiencePerBattle;
            ws.Cells[1, 1, r, 6].AutoFitColumns();
        }

        ws.Cells[1, 1, 1, 6].Style.Font.Bold = true;
        ws.Cells[1, 5, r, 5].Style.Numberformat.Format = "0.0%";
        ws.Cells[1, 6, r, 6].Style.Numberformat.Format = "0";

        await xlsx.SaveAsAsync(new FileInfo(path));
    }
}
