using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mnema.Common.Extensions;

namespace Mnema.Metadata.Mangabaka;

internal class MangabakaDbContext(DbContextOptions<MangabakaDbContext> options): DbContext(options)
{

    public DbSet<MangabakaSeries> Series { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Genres)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonOptions));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Authors)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonOptions));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Publishers)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<MangabakaPublisher>() : JsonSerializer.Deserialize<List<MangabakaPublisher>>(v, JsonOptions));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Artists)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonOptions));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Links)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonOptions));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Titles)
            .HasColumnType("TEXT")
            .IsRequired(false)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<MangabakaTitle>() : JsonSerializer.Deserialize<List<MangabakaTitle>>(v, JsonOptions) ?? new List<MangabakaTitle>());

        modelBuilder.Entity<MangabakaSeries>()
            .Property(e => e.Status)
            .HasConversion(
                v => v.GetEnumMemberValue(),
                v => v.ParseEnumMemberValue<MangabakaPublicationStatus>()
            );

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.TagsV2)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => string.IsNullOrEmpty(v) ? new List<MangabakaTagV2>() : JsonSerializer.Deserialize<List<MangabakaTagV2>>(v, JsonOptions) ?? new List<MangabakaTagV2>());
    }
}
