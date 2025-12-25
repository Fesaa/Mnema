namespace Mnema.Common;

/// <summary>
/// Represents a three-state logical value.
/// </summary>
/// <remarks>
/// This enum is useful when a boolean value may be explicitly true, explicitly false,
/// or left unspecified. The default value is <see cref="TriState.NotSet"/>.
/// </remarks>
public enum TriState
{
    NotSet = 0,
    True = 1,
    False = 2,
}