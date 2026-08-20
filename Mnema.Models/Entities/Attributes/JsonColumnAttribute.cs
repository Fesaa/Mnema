using System;

namespace Mnema.API.Repositories;

[AttributeUsage(AttributeTargets.Property)]
public class JsonColumnAttribute : Attribute
{
    public string ColumnType { get; set; } = "TEXT";
}
