using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;

namespace Mnema.API;

public interface IPagesRepository
{
    Task<List<PageDto>> GetPageDtosForUser();
    Task<List<Page>> GetPages();
    Task<Page?> GetPageById(Guid id);
    Task<int> GetHighestSort();

    Task DeletePage(Guid id);

    void Add(Page page);
    void Update(Page page);
}

public interface IPagesService
{
    Task UpdatePage(PageDto dto);
    Task SetPageDefaults(Guid pageId, MetadataBag defaults, CancellationToken cancellationToken);
    Task OrderPages(Guid[] ids);
}
