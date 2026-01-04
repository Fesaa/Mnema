using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;

namespace Mnema.Server.Logging;

public static class SerilogOptions
{
    public const string OutputTemplate =
        "[Mnema] [{@t:yyyy-MM-dd HH:mm:ss.fff zzz}] ({SourceContext}) [{@l}] {@m:lj}\n{@x}";

    public const string LogFile = "config/logs/mnema.log";

    private static readonly LoggingLevelSwitch LogLevelSwitch = new();

    public static LoggerConfiguration CreateConfig(HostBuilderContext context, LoggerConfiguration configuration)
    {
        return configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(new ExpressionTemplate(OutputTemplate))
            .WriteTo.File(LogFile, rollingInterval: RollingInterval.Day)
            .Filter.ByIncludingOnly(ShouldIncludeLogStatement);
    }

    private static bool ShouldIncludeLogStatement(LogEvent e)
    {
        var sourceContext = e.Properties.TryGetValue("SourceContext", out var property)
            ? property.ToString().Replace("\"", string.Empty)
            : string.Empty;

        if (sourceContext == "LuckyPennySoftware.AutoMapper.License")
            return false;

        if (sourceContext != "Serilog.AspNetCore.RequestLoggingMiddleware")
            return true;

        return LogLevelSwitch.MinimumLevel <= LogEventLevel.Information;
    }
}