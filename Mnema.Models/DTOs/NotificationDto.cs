using System;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.DTOs;

public class NotificationDto: IDatabaseEntity
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public NotificationColour Colour { get; set; }
    public bool Read { get; set; } = false;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
