using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Services;

internal class PageService(ILogger<PageService> logger, IUnitOfWork unitOfWork, IServiceProvider serviceProvider) : IPagesService
{
    public async Task UpdatePage(PageDto dto)
    {
        if (!IsProviderSupported(dto.Provider))
            throw new NotSupportedException("Provider is not supported. Has been marked obsolete");

        var page = dto.Id.Equals(Guid.Empty) ? null : await unitOfWork.PagesRepository.GetPageById(dto.Id);
        var maxSortValue = await unitOfWork.PagesRepository.GetHighestSort();

        var newPage = page == null;

        page ??= new Page
        {
            Title = dto.Title,
            SortValue = maxSortValue + 1,
            Provider = dto.Provider
        };

        page.Icon = dto.Icon;
        page.CustomRootDir = dto.CustomRootDir;
        page.Provider = dto.Provider;

        if (newPage)
            unitOfWork.PagesRepository.Add(page);
        else
            unitOfWork.PagesRepository.Update(page);

        await unitOfWork.CommitAsync();
    }

    public async Task SetPageDefaults(Guid pageId, MetadataBag defaults, CancellationToken cancellationToken)
    {
        var page = await unitOfWork.PagesRepository.GetPageById(pageId);
        if (page == null)
            throw new NotFoundException();

        var repository = serviceProvider.GetKeyedService<IContentRepository>(page.Provider);
        if (repository == null)
            throw new NotFoundException();

        var modifiers = await repository.Modifiers(cancellationToken);

        bool isModified = false;

        if (page.DefaultOptions.Count > 0)
        {
            page.DefaultOptions.Clear();
            isModified = true;
        }

        foreach (var option in modifiers)
        {
            if (defaults.TryGetValue(option.Key, out var value))
            {
                page.DefaultOptions.Add(option.Key, value);
                isModified = true;
            }
        }

        if (isModified)
        {
            unitOfWork.PagesRepository.Update(page);
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task OrderPages(Guid[] ids)
    {
        var pages = await unitOfWork.PagesRepository.GetPages();

        foreach (var page in pages)
        {
            var index = ids.IndexOf(page.Id);
            if (index < 0) throw new BadRequestException("Missing id while ordering pages");

            page.SortValue = index;
            unitOfWork.PagesRepository.Update(page);
        }


        await unitOfWork.CommitAsync();
    }

    private static bool IsProviderSupported(Provider provider)
    {
        return typeof(Provider).GetField(provider.ToString())?.GetCustomAttribute<ObsoleteAttribute>() == null;
    }
}
