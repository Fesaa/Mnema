using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface IUrlParserService
{
    Task<ParsedSeriesUrlDto?> ParseUrl(string url, CancellationToken cancellationToken = default);
}
