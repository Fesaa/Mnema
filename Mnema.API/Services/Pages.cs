using Mnema.Models.DTOs.UI;

namespace Mnema.API.Services;

public interface IPagesService
{
    Task UpdatePage(PageDto dto);
    Task OrderPages(Guid[] ids);
}