using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities.User;

public class AuthKey: IDatabaseEntity, IEntityDate
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }
    public MnemaUser User { get; set; }

    public required string Name { get; set; } = string.Empty;

    public required List<string> Roles { get; set; } = [];

    [StringLength(256, MinimumLength = 8)]
    public required string Key { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
