using Mnema.Models.Entities.User;

namespace Mnema.Models.DTOs.User;

public class NotificationDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public NotificationColour Colour { get; set; }
    public bool Read { get; set; } = false;
}