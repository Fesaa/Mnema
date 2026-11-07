using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface ICoverRepository
{
    Task<List<Cover>> GetCovers(DownloadRequestDto request, CancellationToken cancellationToken);
}
