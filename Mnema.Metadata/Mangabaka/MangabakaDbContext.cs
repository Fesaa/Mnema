using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Mnema.Metadata.Mangabaka;

internal class MangabakaDbContext(DbContextOptions<MangabakaDbContext> options): DbContext(options)
{

    public DbSet<MangabakaSeries> Series { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Genres)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Authors)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Publishers)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrEmpty(v) ? new List<MangabakaPublisher>() : JsonSerializer.Deserialize<List<MangabakaPublisher>>(v, JsonSerializerOptions.Default));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Artists)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default));

        modelBuilder.Entity<MangabakaSeries>()
            .Property(s => s.Links)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default));
    }
}
