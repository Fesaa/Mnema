using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.UI;

namespace Mnema.Services;

public class PageService(ILogger<PageService> logger, IUnitOfWork unitOfWork): IPagesService
{

    public async Task UpdatePage(PageDto dto)
    {
        List<ModifierDto> modifiers = [];

        var idx = 0;
        foreach (var modifier in dto.Modifiers)
        {
            modifier.Sort = idx++;
    
            switch (modifier.Type)
            {
                case ModifierType.DropDown:
                    // Ensure only one modifier value has the default state
                    var foundDefault = false;
                    foreach (var value in modifier.Values)
                    {
                        if (foundDefault)
                        {
                            value.Default = false;
                        }
                        foundDefault = foundDefault || value.Default;
                    }
                    break;
            
                case ModifierType.Multi:
                    break;
            
                case ModifierType.Switch:
                    // Switch modifiers do not have preset values
                    modifier.Values = [];
                    break;
            
                default:
                    throw new ArgumentOutOfRangeException();
            }

            modifiers.Add(modifier);
        }
        
        
        
        var page = await unitOfWork.PagesRepository.GetPageById(dto.Id);
        var maxSortValue = await unitOfWork.PagesRepository.GetHighestSort();

        var newPage = page == null;
        
        page ??= new Page
        {
            Title = dto.Title,
            SortValue = maxSortValue + 1,
        };

        page.Icon = dto.Icon;
        page.Providers = dto.Providers;
        page.CustomRootDir = dto.CustomRootDir;
        page.Modifiers = modifiers;

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