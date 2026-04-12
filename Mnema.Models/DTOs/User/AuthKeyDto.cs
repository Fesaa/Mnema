using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.DTOs.User;

public class AuthKeyDto: IDatabaseEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];

    [StringLength(256, MinimumLength = 8)]
    public string Key { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
