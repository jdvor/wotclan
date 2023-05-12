using Spectre.Console.Cli;
using WotClan.Personnel;

var fix1 = new FetchCommand();
var fix2 = new FetchCommand.Settings();

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<FetchCommand>("fetch")
        .WithDescription("fetch desc")
        .WithExample(new[] { "fetch", "500046758" });
});
await app.RunAsync(args);
