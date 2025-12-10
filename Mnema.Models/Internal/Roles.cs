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

    public static ImmutableArray<string> AllRoles =
    [
        ManagePages,
        ManageSettings,
        Subscriptions,
    ];
}