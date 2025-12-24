using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.UI;

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
    public Task<List<Page>> GetPages()
    {
        return ctx.Pages.ToListAsync();
    }

    public Task<Page?> GetPageById(Guid id)
    {
        return ctx.Pages.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> GetHighestSort()
    {
        var result = await ctx.Pages
            .Select(p => (int?)p.SortValue)
            .MaxAsync();
    
        return result ?? 0;
    }

    public Task DeletePage(Guid id)
    {
        return ctx.Pages
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();
    }

    public void Add(Page page)
    {
        ctx.Pages.Add(page).State = EntityState.Added;
    }

    public void Update(Page page)
    {
        ctx.Pages.Add(page).State = EntityState.Modified;
    }
}