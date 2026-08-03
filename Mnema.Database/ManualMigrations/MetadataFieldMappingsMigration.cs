using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.Models.DTOs;
using Mnema.Models.Enums;

namespace Mnema.Database.ManualMigrations;

public class MetadataFieldMappingsMigration: ManualMigration
{
    protected override string MigrationName => nameof(MetadataFieldMappingsMigration);
    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        var preferences = await ctx.Preferences.SingleAsync();

        preferences.MetadataFieldMappings = preferences.TagMappings
            .SelectMany(tm => new List<MetadataFieldMappingDto>()
            {
                new()
                {
                    SourceType = MetadataFieldType.Genre,
                    DestinationType = MetadataFieldType.Genre,
                    SourceValue = tm.OriginTag,
                    DestinationValue = tm.DestinationTag,
                    ExcludeFromSource = true,
                },
                new()
                {
                    SourceType = MetadataFieldType.Tag,
                    DestinationType = MetadataFieldType.Tag,
                    SourceValue = tm.OriginTag,
                    DestinationValue = tm.DestinationTag,
                    ExcludeFromSource = true,
                }
            })
            .Concat(preferences.ConvertToGenreList.Select(g => new MetadataFieldMappingDto
            {
                SourceType = MetadataFieldType.Tag,
                DestinationType = MetadataFieldType.Genre,
                SourceValue = g,
                DestinationValue = g,
                ExcludeFromSource = true
            }))
            .ToList();


        await ctx.SaveChangesAsync();
    }
}
