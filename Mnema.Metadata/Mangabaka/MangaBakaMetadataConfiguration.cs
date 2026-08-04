using Mnema.API;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models;
using Mnema.Models.DTOs.UI;

namespace Mnema.Metadata.Mangabaka;

public enum LinkFilterMode
{
    Include,
    Exclude,
}

public enum LinkFilterType
{
    Hostname,
    Language,
}

public record LinkFilter(LinkFilterMode Mode, LinkFilterType Type, string Value)
{
    public LinkFilterMode Mode { get; set; } = Mode;
    public LinkFilterType Type { get; set; } = Type;
    public string Value { get; set; } = Value;

    internal bool Matches(MangabakaLinkV2 link)
    {
        return Type switch
        {
            LinkFilterType.Language => link.Language == Value,
            LinkFilterType.Hostname =>
                Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) &&
                uri.Host == Value,
            _ => false
        };
    }

    internal static bool IsAllowed(MangabakaLinkV2 link, IEnumerable<LinkFilter> filters)
    {
        var matchingFilters = filters.Where(f => f.Matches(link)).ToList();

        if (matchingFilters.Any(f => f.Mode == LinkFilterMode.Include))
            return true;

        return matchingFilters.Count == 0;
    }

    internal static bool IsHostnameAllowed(string hostname, IEnumerable<LinkFilter> filters)
    {
        hostname = hostname.RemovePrefix("www");

        var matching = filters
            .Where(f => f.Type == LinkFilterType.Hostname)
            .Where(f => f.Value.Equals(hostname, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matching.Any(f => f.Mode == LinkFilterMode.Include))
            return true;

        return matching.Count == 0;
    }
}

internal class MangaBakaMetadataConfiguration: IConfigurationProvider
{
    internal static readonly IMetadataKey<List<LinkFilter>> LinkFilters = MetadataKeys.JsonArray<LinkFilter>(nameof(LinkFilters));
    internal static readonly IMetadataKey<IEnumerable<string>> SeriesNameLanguagePriority = MetadataKeys.Strings(nameof(SeriesNameLanguagePriority));
    internal static readonly IMetadataKey<IEnumerable<string>> LocalizedSeriesNameLanguagePriority = MetadataKeys.Strings(nameof(LocalizedSeriesNameLanguagePriority));

    public Task<List<FormFieldDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<FormFieldDefinition>
        {
            new CommaSeparatedValuesFieldDefinition
            {
                Key = SeriesNameLanguagePriority.Key,
                ForceSingle = true,
            },
            new CommaSeparatedValuesFieldDefinition
            {
                Key = LocalizedSeriesNameLanguagePriority.Key,
                ForceSingle = true,
            },
            new ArrayFieldDefinition
            {
                Key = LinkFilters.Key,
                Inline = true,
                WikiLink = WikiLinks.MetadataProvidersMangaBaka,
                Controls = [
                    FormFieldDefinitions.EnumDropDown<LinkFilterMode>(nameof(LinkFilter.Mode), "link-filter-mode-pipe") with
                    {
                        ForceEditMode = true,
                        HideText = true,
                    },
                    FormFieldDefinitions.EnumDropDown<LinkFilterType>(nameof(LinkFilter.Type), "link-filter-type-pipe") with
                    {
                        ForceEditMode = true,
                        HideText = true,
                    },
                    new TextFieldDefinition
                    {
                        Field = nameof(LinkFilter.Value),
                        Validators = new FormValidatorsBuilder()
                            .WithRequired()
                            .WithServerSideValidation("Settings/validate-link-filter")
                            .Build(),
                        HideText = true,
                        ForceEditMode = true,
                    },
                ]
            }
        });
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken) => Task.CompletedTask;
}
