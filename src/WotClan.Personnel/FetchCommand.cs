namespace WotClan.Personnel;

using Spectre.Console.Cli;
using System.Text.Json;
using System.ComponentModel;

public sealed class FetchCommand : AsyncCommand<FetchCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        using var http = CreateHttp(settings.Region, settings.ClanId);
        var query = Query(settings.ClanId, settings.TimeFrame, settings.BattleType);

        try
        {
            var bytes = await http.GetByteArrayAsync(query, ct);
            var resp = JsonSerializer.Deserialize<PersonnelResponse>(bytes)!;
            var outputType = ResolveOutputType(settings);
            switch (outputType)
            {
                case OutputType.Csv:
                    var filePath1 = ResolveFilePath(outputType, settings);
                    await Csv.WriteAsync(resp, filePath1);
                    Console.WriteLine(filePath1);
                    break;

                case OutputType.Xlsx:
                    var filePath2 = ResolveFilePath(outputType, settings);
                    await Xlsx.WriteAsync(resp, filePath2);
                    Console.WriteLine(filePath2);
                    break;

                default:
                    Terminal.Write(resp);
                    break;
            }

            return ExitCode.Success;
        }
        catch (TaskCanceledException e) when (e.CancellationToken == ct)
        {
            // expected
            return ExitCode.Success;
        }
        catch (HttpRequestException e)
        {
            Terminal.Write(e, "HTTP request has failed");
            return ExitCode.HttpFailure;
        }
        catch (TaskCanceledException e)
        {
            Terminal.Write(e, "HTTP request timeout");
            return ExitCode.HttpTimeout;
        }
        catch (JsonException e)
        {
            Terminal.Write(e, "response deserialization has failed");
            return ExitCode.DeserializationFailure;
        }
        catch (Exception e)
        {
            Terminal.Write(e, "generic error");
            return ExitCode.Failure;
        }
    }

    private static HttpClient CreateHttp(string region, int clanId)
    {
        var http = new HttpClient();
        http.BaseAddress = new Uri("https://eu.wargaming.net");
        http.DefaultRequestHeaders.Add("Accept", Constants.AcceptsJson);
        http.DefaultRequestHeaders.Add("Accept-Language", Constants.AcceptsEnglish);
        http.DefaultRequestHeaders.Add("Accept-Encoding", Constants.AcceptsEncoding);
        http.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
        http.DefaultRequestHeaders.Add("Referer", $"https://{region}.wargaming.net/clans/wot/{clanId}/players/");
        http.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
        return http;
    }

    private static string Query(int clanId, TimeFrame timeFrame, BattleType battleType)
    {
        var tf = timeFrame.ToHttp();
        var bt = battleType.ToHttp();
        // offset=0&limit=100&
        return $"/clans/wot/{clanId}/api/players/?timeframe={tf}&battle_type={bt}";
    }

    private static OutputType ResolveOutputType(Settings settings)
    {
        static void Verify(string path, string endsWith)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var ext = Path.GetExtension(path)?.ToLowerInvariant();
                if (ext != endsWith)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        switch (settings.OutputType)
        {
            case OutputType.Csv:
                Verify(settings.OutputFile, ".csv");
                return OutputType.Csv;

            case OutputType.Xlsx:
                Verify(settings.OutputFile, ".csv");
                return OutputType.Xlsx;

            default:
                if (!string.IsNullOrEmpty(settings.OutputFile))
                {
                    var ext = Path.GetExtension(settings.OutputFile)?.ToLowerInvariant();
                    return ext switch
                    {
                        ".csv" => OutputType.Csv,
                        ".xlsx" => OutputType.Xlsx,
                        _ => OutputType.Table,
                    };
                }

                return OutputType.Table;
        }
    }

    private static string ResolveFilePath(OutputType effectiveOutputType, Settings settings)
    {
        if (!string.IsNullOrEmpty(settings.OutputFile))
        {
            return Path.GetFullPath(settings.OutputFile);
        }

        switch (effectiveOutputType)
        {
            case OutputType.Csv:
            case OutputType.Xlsx:
                var directory = Path.GetFullPath(settings.OutputDirectory);
                var fileName = CreateFileName(effectiveOutputType, settings);
                return Path.Combine(directory, fileName);

            default:
                return string.Empty;
        }
    }

    private static string CreateFileName(OutputType effectiveOutputType, Settings settings)
    {
        var now = DateTime.UtcNow;
        var ext = effectiveOutputType switch
        {
            OutputType.Xlsx => "xlsx",
            _ => "csv",
        };
        var bt = settings.BattleType switch
        {
            BattleType.Default => "default",
            BattleType.Random => "random",
            BattleType.Skirmish => "skirmish",
            BattleType.Advance => "advance",
            BattleType.Team => "team",
            BattleType.GlobalMap => "gm",
            _ => string.Empty,
        };
        var tf = settings.TimeFrame switch
        {
            TimeFrame.AllAvailable => "all",
            TimeFrame.Days28 => "28d",
            TimeFrame.Days7 => "7d",
            TimeFrame.Day => "1d",
            _ => string.Empty,
        };
        return $"{now:yyyy-MM-dd}_{now:HHmmss}_{bt}_{tf}.{ext}";
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[ClanId]")]
        [Description("Clan ID")]
        public int ClanId { get; set; }

        [CommandOption("-b|--battle-type")]
        [Description("Battle type filter. Valid values are: ...")]
        public BattleType BattleType { get; set; } = BattleType.Default;

        [CommandOption("-t|--timeframe")]
        [Description("Time frame. Valid values are: ...")]
        [DefaultValue("AllAvailable")]
        public TimeFrame TimeFrame { get; set; } = TimeFrame.AllAvailable;

        [CommandOption("-r|--region")]
        [Description("WoT Region. Valid values are: eu, ru, na, asia.")]
        [DefaultValue("eu")]
        public string Region { get; set; } = "eu";

        [CommandOption("-o|--output-type")]
        [Description("")]
        public OutputType OutputType { get; set; } = OutputType.None;

        [CommandOption("-f|--output-file")]
        [Description("")]
        public string OutputFile { get; set; } = string.Empty;

        [CommandOption("-d|--output-directory")]
        [Description("")]
        [DefaultValue("./")]
        public string OutputDirectory { get; set; } = "./";
    }
}
