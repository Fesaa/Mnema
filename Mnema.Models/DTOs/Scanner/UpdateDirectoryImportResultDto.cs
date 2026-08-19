namespace Mnema.Models.DTOs.Scanner;

public sealed record UpdateDirectoryImportResultDto
{
    public required string ParsedSeriesName { get; set; }
    public required int ParsedHardcoverId { get; set; }
    public required int ParsedMangaBakaId { get; set; }
}
