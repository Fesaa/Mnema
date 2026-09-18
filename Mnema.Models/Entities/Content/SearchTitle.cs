using System;
using Microsoft.EntityFrameworkCore;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities.Content;

[Index(nameof(NormalizedTitle), IsUnique = true)]
public class SearchTitle: IDatabaseEntity
{
    public Guid Id { get; set; }
    public Guid MonitoredSeriesId { get; set; }

    public string Title { get; set; }
    [NormalizedFrom(nameof(Title))]
    public string NormalizedTitle { get; set; }
}
