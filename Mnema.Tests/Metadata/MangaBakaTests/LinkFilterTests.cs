using Mnema.Metadata.Mangabaka;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Tests.Metadata.MangaBakaTests;

using Xunit;

public class LinkFilterTests
{
    [Fact]
    public void IsAllowed_NoMatchingFilters_AllowsLink()
    {
        var link = CreateLink("https://example.com", "en");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "blocked.com")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_MatchingExcludeFilter_BlocksLink()
    {
        var link = CreateLink("https://blocked.com/manga/1", "en");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "blocked.com")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_MatchingIncludeFilter_AllowsLink()
    {
        var link = CreateLink("https://allowed.com/manga/1", "en");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "allowed.com")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_IncludeAndExcludeMatch_IncludeWins()
    {
        var link = CreateLink("https://example.com/manga/1", "en");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "example.com"),
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "example.com")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_LanguageFilter_MatchingExclude_BlocksLink()
    {
        var link = CreateLink("https://example.com", "jp");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "jp")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_LanguageFilter_MatchingInclude_AllowsLink()
    {
        var link = CreateLink("https://example.com", "en");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Language, "en")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_NonMatchingLanguageFilter_DoesNotBlock()
    {
        var link = CreateLink("https://example.com", "en");
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "jp")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_LanguageBlockedButHostnameIncluded_AllowsLink()
    {
        var link = CreateLink("https://anilist.co/manga/123", "jp");

        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "jp"),
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "anilist.co")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_LanguageBlockedAndNoHostnameOverride_BlocksLink()
    {
        var link = CreateLink("https://example.com/manga/123", "jp");

        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "jp"),
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "anilist.co")
        };

        var result = LinkFilter.IsAllowed(link, filters);

        Assert.False(result);
    }

    [Fact]
    public void IsHostnameAllowed_NoMatchingFilters_AllowsHostname()
    {
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "blocked.com")
        };

        var result = LinkFilter.IsHostnameAllowed("example.com", filters);

        Assert.True(result);
    }

    [Fact]
    public void IsHostnameAllowed_MatchingExclude_BlocksHostname()
    {
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "blocked.com")
        };

        var result = LinkFilter.IsHostnameAllowed("blocked.com", filters);

        Assert.False(result);
    }

    [Fact]
    public void IsHostnameAllowed_MatchingInclude_AllowsHostname()
    {
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "example.com")
        };

        var result = LinkFilter.IsHostnameAllowed("example.com", filters);

        Assert.True(result);
    }

    [Fact]
    public void IsHostnameAllowed_IncludeAndExcludeMatch_IncludeWins()
    {
        var filters = new[]
        {
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "example.com"),
            new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "example.com")
        };

        var result = LinkFilter.IsHostnameAllowed("example.com", filters);

        Assert.True(result);
    }

    [Fact]
    public void Matches_HostnameMatchingUrl_ReturnsTrue()
    {
        var filter = new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "example.com");
        var link = CreateLink("https://example.com/path", "en");

        Assert.True(filter.Matches(link));
    }

    [Fact]
    public void Matches_HostnameDifferentUrl_ReturnsFalse()
    {
        var filter = new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "example.com");
        var link = CreateLink("https://other.com/path", "en");

        Assert.False(filter.Matches(link));
    }

    [Fact]
    public void Matches_LanguageMatchingLanguage_ReturnsTrue()
    {
        var filter = new LinkFilter(LinkFilterMode.Include, LinkFilterType.Language, "en");
        var link = CreateLink("https://example.com", "en");

        Assert.True(filter.Matches(link));
    }

    [Fact]
    public void Matches_InvalidUrlForHostname_ReturnsFalse()
    {
        var filter = new LinkFilter(LinkFilterMode.Include, LinkFilterType.Hostname, "example.com");
        var link = CreateLink("not-a-url", "en");

        Assert.False(filter.Matches(link));
    }

    private static MangabakaLinkV2 CreateLink(string url, string language)
    {
        return new MangabakaLinkV2
        {
            Url = url,
            Language = language
        };
    }
}
