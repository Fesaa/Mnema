namespace Mnema.Models.DTOs.Scanner;

public record StartScanDto
{
    public required string RootDir { get; set; }
}
