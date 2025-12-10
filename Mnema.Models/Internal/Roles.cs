using System.Collections.Immutable;

namespace Mnema.Models.Internal;

public static class Roles
{
    public const string ManagePages = nameof(ManagePages);
    public const string ManageSettings = nameof(ManageSettings);

    public static ImmutableArray<string> AllRoles =
    [
        ManagePages,
        ManageSettings,
    ];
}