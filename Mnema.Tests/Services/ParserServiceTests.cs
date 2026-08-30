using Mnema.API.Content;
using Mnema.Models.Enums;
using Mnema.Services;

namespace Mnema.Tests.Services;

/// <summary>
/// This class should only contain test cases for which Mnema difference from Kavita. The rest is tested in Kavita
/// </summary>
public class ParserServiceTests
{

    private readonly IParserService _parserService = new ParserService();

    [Theory]
    [InlineData(" Escape from the Seventh Night 001-005 as v01 (Digital-Compilation) (Oak)", "Escape from the Seventh Night")]
    public void ParserMangaSeries(string input, string expected)
    {
        var parsedSeries = _parserService.ParseSeries(input, ContentFormat.Manga);
        Assert.Equal(expected, parsedSeries);
    }

}
