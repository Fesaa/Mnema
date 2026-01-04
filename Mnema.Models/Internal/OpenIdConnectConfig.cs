namespace Mnema.Models.Internal;

public class OpenIdConnectConfig
{
    public required string Authority { get; init; }
    public required string ClientId { get; init; }
    public required string Secret { get; init; }

    public bool Valid => !string.IsNullOrWhiteSpace(Authority)
                         && !string.IsNullOrWhiteSpace(ClientId)
                         && !string.IsNullOrWhiteSpace(Secret);
}