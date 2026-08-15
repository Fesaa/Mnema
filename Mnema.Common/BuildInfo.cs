using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mnema.Common;

public static class BuildInfo
{
    private static readonly Assembly TargetAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

    public static readonly Version Version = TargetAssembly.GetName().Version ?? new Version(0, 0, 0, 0);
    public static string AppName { get; } = TargetAssembly.GetName().Name ?? "Mnema";

    public static string AppIdentifier { get; } = $"{AppName}/{Version}";

    public static string FrameworkDescription { get; } = RuntimeInformation.FrameworkDescription;
}
