namespace Mnema.Models.Entities;

public interface IEntityDate
{
    DateTime CreatedUtc { get; set; }
    DateTime LastModifiedUtc { get; set; }
}