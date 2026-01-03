using System.Collections.Generic;

namespace Mnema.Models.Publication;

public sealed record Person
{
    public required string Name { get; set; }
    public required IList<PersonRole> Roles { get; set; }
}