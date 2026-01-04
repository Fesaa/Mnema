using System;
using System.Collections.Generic;

namespace Mnema.Models.DTOs.User;

public class UserDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required IList<string> Roles { get; set; }
}