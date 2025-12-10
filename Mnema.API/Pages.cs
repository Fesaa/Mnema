using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.UI;

namespace Mnema.API;

public interface IPagesRepository
{
    Task<List<PageDto>> GetPageDtosForUser(Guid userId);
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
    Task OrderPages(Guid[] ids);
}