namespace Mnema.Models.DTOs.Content;

public sealed record FileInfoDto
{
    public required string Path { get; set; }
    public required string Volume { get; set; }
    public required string Chapter { get; set; }
    public required FileMetadataDto? Metadata { get; set; }
}
