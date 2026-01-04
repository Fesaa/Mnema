using System;

namespace Mnema.Models.Entities.User;

public enum NotificationColour
{
    Primary = 0,
    Secondary = 1,
    Warning = 2,
    Error = 3
}

public class Notification : IEntityDate
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public NotificationColour Colour { get; set; }
    public bool Read { get; set; } = false;

    public required Guid UserId { get; set; }
    public virtual MnemaUser User { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}