using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.UI;

public class PageDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = string.Empty;
    public required int SortValue { get; set; }
    public required Provider Provider { get; set; }
    public IList<ModifierDto>? Modifiers { get; set; }
    public string CustomRootDir { get; set; } = string.Empty;
    
    public DownloadMetadata? Metadata { get; set; }

}

public sealed record DownloadMetadata(List<DownloadMetadataDefinition> Definitions);

public sealed record DownloadMetadataDefinition
{
    public required string Key { get; set; }

    public required FormType FormType { get; set; }

    public bool Advanced { get; set; } = false;
    
    public string DefaultOption { get; set; } = string.Empty;

    public List<KeyValue> Options { get; set; } = [];
}

public sealed record KeyValue(string Key, string Value);

public enum FormType
{
    Switch = 0,
    Dropdown = 1,
    MultiSelect = 2,
    Text = 3,
}