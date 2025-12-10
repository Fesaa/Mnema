using Microsoft.EntityFrameworkCore;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.UI;
using Mnema.Models.Entities.User;

namespace Mnema.Database;

public class MnemaDataContext(DbContextOptions options): DbContext(options)
{
    
    public DbSet<MnemaUser> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ServerSetting> ServerSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {

        builder.Entity<UserPreferences>()
            .PrimitiveCollection(p => p.ConvertToGenreList)
            .HasDefaultValue(new List<string>());
        builder.Entity<UserPreferences>()
            .PrimitiveCollection(p => p.BlackListedTags)
            .HasDefaultValue(new List<string>());
        builder.Entity<UserPreferences>()
            .PrimitiveCollection(p => p.WhiteListedTags)
            .HasDefaultValue(new List<string>());
        builder.Entity<UserPreferences>()
            .ComplexCollection(p => p.AgeRatingMappings, b => b.ToJson());
        builder.Entity<UserPreferences>()
            .ComplexCollection(p => p.TagMappings, b => b.ToJson());

        builder.Entity<Page>()
            .ComplexCollection(p => p.Modifiers, b => b.ToJson());
        builder.Entity<Page>()
            .PrimitiveCollection(p => p.Providers);
        builder.Entity<Page>()
            .PrimitiveCollection(p => p.Dirs);
        builder.Entity<Page>()
            .HasMany(p => p.Users);

        builder.Entity<MnemaUser>()
            .HasMany(u => u.Pages);

    }
}