using System;
using System.Collections.Generic;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public class CreateOrUpdateMonitoredSeriesDto
{
    public Guid Id { get; set; } = Guid.Empty;

    public required string Title { get; set; }

    public List<string> ValidTitles { get; set; } = [];

    public List<Provider> Providers { get; set; } = [];

    public required string BaseDir { get; set; }

    public ContentFormat ContentFormat { get; set; }
    public Format Format { get; set; }

    public string HardcoverId { get; set; }
    public string MangaBakaId { get; set; }
    public string TitleOverride { get; set; }

}
