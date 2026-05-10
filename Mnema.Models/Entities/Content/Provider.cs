using System;

namespace Mnema.Models.Entities.Content;

public enum Provider
{
    Nyaa = 0,
    Mangadex = 1,
    Webtoons = 2,
    Dynasty = 3,
    [Obsolete("Bato died")]
    Bato = 4,
    [Obsolete("Weebdex is shutting down 07/04/2026")]
    Weebdex = 5,
    [Obsolete("Dumb anti scraper, not worth my time")]
    Comix = 6,
    Kagane = 7,
    MadoKami = 8,
    /// <remarks>Forgive me for my transgression, MTL is bad.</remarks>
    AthreaScans,
}

public static class ProviderExtensions
{
    public static bool IsDirectDownload(this Provider provider) => provider switch
    {
        Provider.MadoKami => true,
        _ => false
    };
}

public enum MetadataProvider
{
    Hardcover = 0,
    Mangabaka = 1,
    /// <summary>
    /// Metadata from <see cref="Provider"/>
    /// </summary>
    /// <remarks>The typo is intentional at this point, or I need to write a migration....</remarks>
    Upsteam = 2,
}
