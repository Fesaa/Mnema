using System;
using System.Reflection;

namespace Mnema.Common;

public static class BuildInfo
{
    private static readonly Assembly TargetAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

    public static readonly Version Version = TargetAssembly.GetName().Version ?? new Version(0, 0, 0, 0);
    public static string AppName { get; } = TargetAssembly.GetName().Name ?? "Mnema";

    public static string InformationalVersion { get; } =
        TargetAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Version.ToString();

    public static string AppIdentifier { get; } = $"{AppName}/{InformationalVersion}";

    public static string FrameworkDescription { get; } = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
}
