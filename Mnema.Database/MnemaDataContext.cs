using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Authentication;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Entities.Scanner;
using Mnema.Models.Publication;

namespace Mnema.Database;

public class MnemaDataContext : DbContext, IDataProtectionKeyContext
{

    public MnemaDataContext(DbContextOptions options) : base(options)
    {
        ChangeTracker.Tracked += OnEntityTracked;
        ChangeTracker.StateChanged += OnEntityStateChanged;
    }

    public DbSet<Preferences> Preferences { get; set; }
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
    public DbSet<AuthKey> AuthKeys { get; set; }
    public DbSet<ProviderSettings> ProviderSettings { get; set; }
    public DbSet<ExternalDownload> ExternalDownloads { get; set; }
    public DbSet<MetadataProviderSettings> MetadataProviderSettings { get; set; }
    public DbSet<ImportScan> ImportScans { get; set; }
    public DbSet<DirectoryImportResult> DirectoryImportResults { get; set; }
    public DbSet<ImportError> ImportErrors { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyJsonColumns();

        builder.Entity<Preferences>()
            .PrimitiveCollection(p => p.ConvertToGenreList)
            .HasDefaultValue(new List<string>());
        builder.Entity<Preferences>()
            .PrimitiveCollection(p => p.BlackListedTags)
            .HasDefaultValue(new List<string>());
        builder.Entity<Preferences>()
            .PrimitiveCollection(p => p.WhiteListedTags)
            .HasDefaultValue(new List<string>());
        builder.Entity<Preferences>()
            .ComplexCollection(p => p.AgeRatingMappings, b => b.ToJson());
        builder.Entity<Preferences>()
            .ComplexCollection(p => p.TagMappings, b => b.ToJson());
        builder.Entity<Preferences>()
            .ComplexCollection(p => p.MetadataFieldMappings, b => b.ToJson());
        builder.Entity<Preferences>()
            .ComplexCollection(p => p.LinkFilters, b => b.ToJson());

        builder.Entity<Connection>()
            .PrimitiveCollection(c => c.FollowedEvents)
            .HasDefaultValue(new List<ConnectionEvent>());

        builder.Entity<MonitoredSeries>()
            .PrimitiveCollection(m => m.ValidTitles);

        builder.Entity<AuthKey>()
            .PrimitiveCollection(k => k.Roles)
            .HasDefaultValue(new List<string>());

        builder.Entity<DirectoryImportResult>()
            .PrimitiveCollection(p => p.Files)
            .HasDefaultValue(new List<string>());

        builder.Entity<DirectoryImportResult>()
            .HasOne(x => x.MonitoredSeries)
            .WithMany()
            .HasForeignKey(x => x.MonitoredSeriesId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ImportScan>()
            .HasMany(x => x.DirectoryImportResults)
            .WithOne(x => x.ImportScan)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ImportScan>()
            .HasMany(x => x.ImportErrors)
            .WithOne(x => x.ImportScan)
            .OnDelete(DeleteBehavior.Cascade);
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
