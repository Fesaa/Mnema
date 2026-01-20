using System;

namespace Mnema.Models.Entities.Interfaces;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NormalizedFromAttribute(string propertyName): Attribute
{
    public string PropertyName { get; } = propertyName;
}
