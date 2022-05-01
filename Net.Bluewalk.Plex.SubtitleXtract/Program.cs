using System.Diagnostics;
using Net.Bluewalk.Plex.SubtitleXtract.Core;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
Console.WriteLine($"Plex SubtitleXtract, version {version}");
Console.WriteLine("https://github.com/bluewalk/plex-subtitlextract\n");

var outputTemplate =
    "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] [{SourceContext}] {Message:j}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: outputTemplate, theme: AnsiConsoleTheme.Code)
    .WriteTo.File("logs/log.log", rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate, retainedFileCountLimit: 7)
    .CreateLogger();

using var extractor = new Extractor();
extractor.Run();