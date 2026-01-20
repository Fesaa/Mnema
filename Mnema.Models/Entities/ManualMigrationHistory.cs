using System;

namespace Mnema.Models.Entities;

public class ManualMigrationHistory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime RanAt { get; set; } = DateTime.UtcNow;
}
