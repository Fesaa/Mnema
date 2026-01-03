using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.UI;

namespace Mnema.Services;

internal class PageService(ILogger<PageService> logger, IUnitOfWork unitOfWork): IPagesService
{

    public async Task UpdatePage(PageDto dto)
    {
        var page = dto.Id.Equals(Guid.Empty) ? null : await unitOfWork.PagesRepository.GetPageById(dto.Id);
        var maxSortValue = await unitOfWork.PagesRepository.GetHighestSort();

        var newPage = page == null;
        
        page ??= new Page
        {
            Title = dto.Title,
            SortValue = maxSortValue + 1,
            Provider = dto.Provider,
        };

        page.Icon = dto.Icon;
        page.CustomRootDir = dto.CustomRootDir;
        page.Provider = dto.Provider;

        if (newPage)
        {
            unitOfWork.PagesRepository.Add(page);
        }
        else
        {
            unitOfWork.PagesRepository.Update(page);
        }

        await unitOfWork.CommitAsync();
    }

    public async Task OrderPages(Guid[] ids)
    {
        var pages = await unitOfWork.PagesRepository.GetPages();

        foreach (var page in pages)
        {
            var index = ids.IndexOf(page.Id);
            if (index < 0) throw new MnemaException("Missing id while ordering pages");

            page.SortValue = index;
            unitOfWork.PagesRepository.Update(page);
        }


        await unitOfWork.CommitAsync();
    }
}