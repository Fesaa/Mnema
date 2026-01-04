using System.ComponentModel.DataAnnotations;

namespace Mnema.Models.Entities;

public class ServerSetting
{
    [Key] public required ServerSettingKey Key { get; set; }

    public required string Value { get; set; }
}