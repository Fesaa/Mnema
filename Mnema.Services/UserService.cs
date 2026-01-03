using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.User;

namespace Mnema.Services;

internal class UserService(ILogger<UserService> logger, IUnitOfWork unitOfWork): IUserService
{

    public async Task UpdatePreferences(Guid userId, UserPreferencesDto dto)
    {
        var pref = await unitOfWork.UserRepository.GetPreferences(userId);
        if (pref == null) throw new UnauthorizedAccessException();

        pref.ImageFormat = dto.ImageFormat;
        pref.CoverFallbackMethod = dto.CoverFallbackMethod;
        pref.BlackListedTags = dto.BlackListedTags.DistinctBy(t => t.ToNormalized()).ToList();
        pref.WhiteListedTags = dto.WhiteListedTags.DistinctBy(t => t.ToNormalized()).ToList();
        pref.ConvertToGenreList = dto.ConvertToGenreList.DistinctBy(g => g.ToNormalized()).ToList();
        pref.AgeRatingMappings = dto.AgeRatingMappings.DistinctBy(arm => arm.Tag.ToNormalized()).ToList();
        pref.TagMappings = dto.TagMappings.DistinctBy(tm => tm.DestinationTag.ToNormalized() + tm.OriginTag.ToNormalized()).ToList();
        pref.PinSubscriptionTitles = dto.PinSubscriptionTitles;

        unitOfWork.UserRepository.Update(pref);

        await unitOfWork.CommitAsync();
    }
}