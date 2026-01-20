using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Entities.UI;
using Mnema.Models.Entities.User;

namespace Mnema.Database;

public sealed class MnemaDataContext : DbContext, IDataProtectionKeyContext
{

    public MnemaDataContext(DbContextOptions options) : base(options)
    {
        ChangeTracker.Tracked += OnEntityTracked;
        ChangeTracker.StateChanged += OnEntityStateChanged;
    }

    public DbSet<MnemaUser> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Page> Pages { get; set; }
    [Obsolete("Use MonitoredSeries")]
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ServerSetting> ServerSettings { get; set; }
    public DbSet<Connection> Connections { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<ContentRelease> ContentReleases { get; set; }
    public DbSet<DownloadClient> DownloadClients { get; set; }
    public DbSet<MonitoredSeries> MonitoredSeries { get; set; }
    public DbSet<MonitoredChapter> MonitoredChapters { get; set; }
    public DbSet<ManualMigrationHistory> ManualMigrationHistory { get; set; }

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
            .HasJsonConversion(new MetadataBag())
            .HasColumnType("TEXT")
            .HasDefaultValue(new MetadataBag());

        builder.Entity<Connection>()
            .Property(c => c.Metadata)
            .HasJsonConversion([])
            .HasColumnType("TEXT")
            .HasDefaultValue(new MetadataBag());
        builder.Entity<Connection>()
            .PrimitiveCollection(c => c.FollowedEvents)
            .HasDefaultValue(new List<ConnectionEvent>());

        builder.Entity<DownloadClient>()
            .Property(d => d.Metadata)
            .HasJsonConversion([])
            .HasColumnType("TEXT")
            .HasDefaultValue(new MetadataBag());

        builder.Entity<MonitoredSeries>()
            .PrimitiveCollection(m => m.ValidTitles);

        builder.Entity<MonitoredSeries>()
            .PrimitiveCollection(m => m.Providers);

        builder.Entity<MonitoredSeries>()
            .Property(s => s.Metadata)
            .HasJsonConversion(new MetadataBag())
            .HasColumnType("TEXT")
            .HasDefaultValue(new MetadataBag());
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

        if (entity.CreatedUtc == DateTime.MinValue)
        {
            entity.CreatedUtc = DateTime.UtcNow;
        }
    }
}
