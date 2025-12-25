using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.UI;
using Mnema.Models.Entities.User;

namespace Mnema.Database;

public sealed class MnemaDataContext: DbContext, IDataProtectionKeyContext
{

    public MnemaDataContext(DbContextOptions options): base(options)
    {
        ChangeTracker.Tracked += OnEntityTracked;
        ChangeTracker.StateChanged += OnEntityStateChanged;
    }
    
    public DbSet<MnemaUser> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ServerSetting> ServerSettings { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

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
            .PrimitiveCollection(p => p.Dirs);
        builder.Entity<Page>()
            .HasMany(p => p.Users);

        builder.Entity<MnemaUser>()
            .HasMany(u => u.Pages);

        builder.Entity<Subscription>()
            .Property(s => s.Metadata)
            .HasJsonConversion(new DownloadMetadataDto())
            .HasColumnType("TEXT")
            .HasDefaultValue(new DownloadMetadataDto());

    }
    
    private static void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        if (e.FromQuery || e.Entry.State != EntityState.Added || e.Entry.Entity is not IEntityDate entity) return;

        entity.LastModifiedUtc = DateTime.UtcNow;

        if (entity.CreatedUtc == DateTime.MinValue)
        {
            entity.CreatedUtc = DateTime.UtcNow;
        }
    }

    private static void OnEntityStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        if (e.NewState != EntityState.Modified || e.Entry.Entity is not IEntityDate entity) return;
        entity.LastModifiedUtc = DateTime.UtcNow;
    }
}