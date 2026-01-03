using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
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

    public Task<MnemaUser?> GetUserByIdOrDefault(Guid id, UserIncludes includes = UserIncludes.Preferences)
    {
        return ctx.Users
            .Includes(includes)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<UserPreferences?> GetPreferences(Guid id)
    {
        return ctx.UserPreferences.Where(p => p.UserId == id).FirstOrDefaultAsync();
    }

    public void Update(UserPreferences pref)
    {
        ctx.UserPreferences.Update(pref).State = EntityState.Modified;
    }
}