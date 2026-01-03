using System.Collections.Immutable;

namespace Mnema.Models.Internal;

public static class Roles
{
    /// <summary>
    /// If the user is allowed to 
    /// </summary>
    public const string ManagePages = nameof(ManagePages);
    /// <summary>
    /// If the user is allowed to change server settings
    /// </summary>
    public const string ManageSettings = nameof(ManageSettings);
    /// <summary>
    /// If the user is allowed to create/use subscription
    /// </summary>
    public const string Subscriptions = nameof(Subscriptions);
    /// <summary>
    /// If the user is allowed to access the HangFire dashboard
    /// </summary>
    public const string HangFire = nameof(HangFire);
    /// <summary>
    /// If the user is allowed to create directories
    /// </summary>
    public const string CreateDirectory = nameof(CreateDirectory);
    public const string ManageExternalConnections = nameof(ManageExternalConnections);

    public static ImmutableArray<string> AllRoles =
    [
        ManagePages,
        ManageSettings,
        Subscriptions,
        HangFire,
        CreateDirectory,
        ManageExternalConnections
    ];
}