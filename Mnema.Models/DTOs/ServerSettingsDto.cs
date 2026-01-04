using System;

namespace Mnema.Models.DTOs;

public class ServerSettingsDto
{
    public int MaxConcurrentTorrents { get; set; }
    public int MaxConcurrentImages { get; set; }
    public string InstalledVersion { get; set; }
    public string FirstInstalledVersion { get; set; }
    public DateTime InstallDate { get; set; }
    public int SubscriptionRefreshHour { get; set; }
    public DateTime LastUpdateDate { get; set; }
}

public class UpdateServerSettingsDto
{
    public int MaxConcurrentTorrents { get; set; }
    public int MaxConcurrentImages { get; set; }
    public int SubscriptionRefreshHour { get; set; }
}
