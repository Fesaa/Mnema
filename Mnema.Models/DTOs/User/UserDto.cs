namespace Mnema.Models.DTOs.User;

public class UserDto
{
    public required Guid Id { get; set; }
    public required IList<string> Roles { get; set; }
}