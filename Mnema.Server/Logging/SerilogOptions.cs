using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;

namespace Mnema.Server.Logging;

public class SerilogOptions
{
    public const string LogFile = "config/logs/mnema-api.log";
    public const bool LogRollingEnabled = true;

    private static readonly LoggingLevelSwitch LogLevelSwitch = new ();
    private static readonly LoggingLevelSwitch MicrosoftLogLevelSwitch = new (LogEventLevel.Error);
    private static readonly LoggingLevelSwitch MicrosoftHostingLifetimeLogLevelSwitch = new (LogEventLevel.Error);
    private static readonly LoggingLevelSwitch AspNetCoreLogLevelSwitch = new (LogEventLevel.Error);

    public static LoggerConfiguration CreateConfig(LoggerConfiguration configuration)
    {
        const string outputTemplate = "[Mnema] [{@t:yyyy-MM-dd HH:mm:ss.fff zzz}] [{@l}] {SourceContext} {@m:lj}\n{@x}";
        return configuration
            .MinimumLevel.ControlledBy(LogLevelSwitch)
            .MinimumLevel.Override("Microsoft", MicrosoftLogLevelSwitch)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", MicrosoftHostingLifetimeLogLevelSwitch)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Internal.WebHost", AspNetCoreLogLevelSwitch)
            .Enrich.FromLogContext()
            .WriteTo.Console(new ExpressionTemplate(outputTemplate))
            .WriteTo.File(LogFile, rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate)
            .Filter.ByIncludingOnly(ShouldIncludeLogStatement);
    }

    private static bool ShouldIncludeLogStatement(LogEvent e)
    {
        var isRequestLoggingMiddleware = e.Properties.ContainsKey("SourceContext") &&
                                         e.Properties["SourceContext"].ToString().Replace("\"", string.Empty) ==
                                         "Serilog.AspNetCore.RequestLoggingMiddleware";

        if (!isRequestLoggingMiddleware) return true;

        return LogLevelSwitch.MinimumLevel <= LogEventLevel.Information;
    }
}