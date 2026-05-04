namespace Mnema.Models.Publication;

public class Cover
{
    public required string Url { get; set; }
    public required string Extension { get; set; }
    public string Volume { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
}
