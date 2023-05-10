namespace WotClan.Personnel;

using OfficeOpenXml;

internal static class Xlsx
{
    public static async Task WriteAsync(PersonnelResponse pr, string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var p = new ExcelPackage();
        var ws = p.Workbook.Worksheets.Add("1");
        ws.Cells["A1"].Value = "This is cell A1";
        await p.SaveAsAsync(new FileInfo(path));
    }
}
