using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.External;
using Mnema.Models.Entities.UI;
using Mnema.Models.Entities.User;

namespace Mnema.Database;

public sealed class MnemaDataContext(DbContextOptions options): DbContext(options), IDataProtectionKeyContext
{
    
    public DbSet<MnemaUser> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ServerSetting> ServerSettings { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<ExternalConnection> ExternalConnections { get; set; }

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
            .HasMany(p => p.Users);

        builder.Entity<MnemaUser>()
            .HasMany(u => u.Pages);

        builder.Entity<Subscription>()
            .Property(s => s.Metadata)
            .HasJsonConversion(new DownloadMetadataDto())
            .HasColumnType("TEXT")
            .HasDefaultValue(new DownloadMetadataDto());
        builder.Entity<Subscription>()
            .Property(s => s.RefreshFrequency)
            .HasDefaultValue(RefreshFrequency.Day);

        builder.Entity<ExternalConnection>()
            .Property(c => c.Metadata)
            .HasJsonConversion([])
            .HasColumnType("TEXT")
            .HasDefaultValue(new MetadataBag());
        builder.Entity<ExternalConnection>()
            .PrimitiveCollection(c => c.FollowedEvents)
            .HasDefaultValue(new List<ExternalConnectionEvent>());

    }
}