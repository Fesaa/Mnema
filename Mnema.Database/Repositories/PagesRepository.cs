using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Database;
using Mnema.Models.DTOs.UI;

namespace Mnema.Database.Repositories;

public class PagesRepository(MnemaDataContext ctx, IMapper mapper): IPagesRepository
{

    public Task<List<PageDto>> GetPageDtosForUser(Guid userId)
    {
        return ctx.Pages
            .Where(p => p.Users.Select(u => u.Id).Contains(userId))
            .ProjectTo<PageDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}