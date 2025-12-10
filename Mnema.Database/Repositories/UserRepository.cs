using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Database;
using Mnema.Database.Extensions;
using Mnema.Models.Entities.User;

namespace Mnema.Database.Repositories;

public class UserRepository(MnemaDataContext ctx, IMapper mapper): IUserRepository
{

    public async Task<MnemaUser> GetUserById(Guid id, UserIncludes includes = UserIncludes.Preferences)
    {
        var user = await ctx.Users
            .Includes(includes)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user != null)
        {
            return user;
        }

        user = new MnemaUser
        {
            Id = id,
            Preferences = new UserPreferences
            {
                LogEmptyDownloads = false,
                ImageFormat = ImageFormat.Upstream,
                CoverFallbackMethod = CoverFallbackMethod.First,
                ConvertToGenreList = [],
                BlackListedTags = [],
                WhiteListedTags = [],
                AgeRatingMappings = [],
                TagMappings = [],
            },
        };

        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        return await GetUserById(id);
    }
}