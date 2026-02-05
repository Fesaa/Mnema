using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mnema.Common;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.DTOs;

public class ConnectionDto: IDatabaseEntity
{
    public Guid Id { get; set; }

    [Required] public ConnectionType Type { get; set; }

    [Required] [MinLength(1)] public string Name { get; set; }

    [Required] public List<ConnectionEvent> FollowedEvents { get; set; }

    [Required] public MetadataBag Metadata { get; set; }
}
