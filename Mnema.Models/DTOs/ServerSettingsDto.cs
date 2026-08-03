using System;
using System.Collections.Generic;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs;

public class ServerSettingsDto
{
    public int MaxConcurrentImages { get; set; }
    public string InstalledVersion { get; set; }
    public string FirstInstalledVersion { get; set; }
    public DateTime InstallDate { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public int AutoDisableProviderAfter { get; set; }
    public bool ImageConversionLossless { get; set; }
    public int ImageConversionQuality { get; set; }
}

public class UpdateServerSettingsDto
{
    public int MaxConcurrentImages { get; set; }
    public int AutoDisableProviderAfter { get; set; }
    public bool ImageConversionLossless { get; set; }
    public int ImageConversionQuality { get; set; }
}
