using System;

namespace Mnema.Database.Interfaces;

public interface ITimeAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime LastModifiedAtUtc { get; set; }
}