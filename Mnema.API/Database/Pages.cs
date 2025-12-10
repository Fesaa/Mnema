using Mnema.Models.DTOs.UI;

namespace Mnema.API.Database;

public interface IPagesRepository
{
    Task<List<PageDto>> GetPageDtosForUser(Guid userId);
}