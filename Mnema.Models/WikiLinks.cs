using System;

namespace Mnema.Models;

public static class WikiLinks
{

    private static readonly bool IsDev =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development") == true;

    public static readonly string WikiBase = IsDev ? "http://localhost:63343/Docs/preview/" : "https://fesaa.github.io/Mnema/";
    public static readonly string NamingFormatDocumentation = WikiBase + "naming.html";
    public static readonly string ServerSettings = WikiBase + "server.html";
    public static readonly string MetadataProvidersMangaBaka = ServerSettings + "#mangabaka";
}
