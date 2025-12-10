using System.Collections.Immutable;

namespace Mnema.Models.Internal;

public class Roles
{
    public const string ManagePages = nameof(ManagePages);

    public static ImmutableArray<string> AllRoles =
    [
        ManagePages,
    ];
}