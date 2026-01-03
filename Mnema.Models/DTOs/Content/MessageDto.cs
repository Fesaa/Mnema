using System.Collections.Generic;
using System.Text.Json.Nodes;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public class MessageDto
{
    public required Provider Provider { get; set; }
    public required string ContentId  { get; set; }
    public required MessageType Type { get; set; }
    public JsonNode? Data { get; set; }
}

public enum MessageType
{
    ListContent,
    FilterContent,
    StartDownload
}

public class ListContentData
{
    public required string Label { get; set; }
    public bool Selected { get; set; } = false;
    public string? SubContentId  { get; set; }
    public List<ListContentData> Children { get; set; } = [];
}