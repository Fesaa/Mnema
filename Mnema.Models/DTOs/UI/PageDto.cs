using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.UI;

public class PageDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = string.Empty;
    public required int SortValue { get; set; }
    public required IList<Provider> Providers { get; set; }
    public required IList<ModifierDto> Modifiers { get; set; }
    public required IList<string> Dirs { get; set; }
    public string CustomRootDir { get; set; } = string.Empty;

}