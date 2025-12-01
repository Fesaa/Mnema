
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.Publication;

public abstract partial class Publication(IServiceScope scope, Provider provider)
{

    private ILogger<Publication> Logger { get; init; } = scope.ServiceProvider.GetRequiredService<ILogger<Publication>>();
    private IRepository Repository { get; init; } = scope.ServiceProvider.GetRequiredKeyedService<IRepository>(provider);

    
}