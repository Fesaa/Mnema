using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IChapterCombineService
{
    Task<ChapterCombineResult> Combine(MonitoredSeries series, IReadOnlyList<MonitoredChapter> chapters,
        CancellationToken cancellationToken = default);
}
