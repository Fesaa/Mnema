using System.Text.Json.Serialization;
using GraphQL.Client.Abstractions.Utilities;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Hardcover;

public record HardcoverEntity
{
    public int Id { get; init; }
}

public sealed record HardcoverSeries : HardcoverEntity
{
    public string Name { get; init; }
    public string Slug { get; init; }
    public string? Description { get; init; }
    public bool? IsCompleted { get; init; }
    public int BooksCount { get; init; }
    public HardcoverAuthor? Author { get; init; }
    public List<HardcoverBookSeries> BookSeries { get; init; } = [];

    public List<Person> People()
    {
        List<Person> people = [];
        if (Author != null)
        {
            people.Add(new Person
            {
                Name = Author.Name,
                Roles = [PersonRole.Writer]
            });
        }

        people.AddRange(BookSeries.Select(b => b.Book)
            .SelectMany(b => b.Contributions)
            .Where(c => c.Role != null)
            .Select(c => new Person
            {
                Name = c.Author.Name,
                Roles = [c.Role!.Value]
            })
        );

        return people.DistinctBy(p => p.Name).ToList();
    }
}

public sealed record HardcoverAuthor : HardcoverEntity
{
    public string Slug { get; init; }
    public string Name { get; init; }
    public List<string> AlternateNames { get; init; } = [];
    public string? Bio { get; init; }
    public int? GenderId { get; init; }
    public HardcoverImage? Image { get; init; }
}

public sealed record HardcoverBookSeries
{
    public float? Position { get; init; }
    public bool Featured { get; init; }
    public HardcoverBook Book { get; init; }
}

public sealed record HardcoverBook : HardcoverEntity
{
    public string Title { get; init; }
    public List<string> AlternativeTitles { get; init; } = [];
    public string? Description { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public int? ReleaseYear { get; init; }
    public string Slug { get; init; }
    public HardcoverImage? Image { get; init; }
    public List<HardcoverTagging> Taggings { get; init; } = [];
    public List<HardoverContribution> Contributions { get; init; } = [];
    [JsonPropertyName("users_read_count")]
    public long UserReadCount { get; init; }
}

public sealed record HardoverContribution
{
    public const string AuthorValue = nameof(Author);
    public const string Illustrator = nameof(Illustrator);
    public const string Translator = nameof(Translator);
    public const string Editor = nameof(Editor);
    public const string Letterer = nameof(Letterer);
    public const string Narrator = nameof(Narrator);
    public const string Foreword = nameof(Foreword);
    public const string Afterword = nameof(Afterword);
    public const string CoverArtist = "Cover Artist";


    /// <summary>
    /// When null, author
    /// </summary>
    public string? Contribution { get; init; }
    public HardcoverAuthor Author { get; init; }

    public PersonRole? Role
    {
        get
        {
            if (Contribution is null ||
                string.Equals(Contribution, AuthorValue, StringComparison.OrdinalIgnoreCase))
                return PersonRole.Writer;

            if (string.Equals(Contribution, Illustrator, StringComparison.OrdinalIgnoreCase))
                return PersonRole.Colorist;

            if (string.Equals(Contribution, Translator, StringComparison.OrdinalIgnoreCase))
                return PersonRole.Translator;

            if (string.Equals(Contribution, Editor, StringComparison.OrdinalIgnoreCase))
                return PersonRole.Editor;

            if (string.Equals(Contribution, CoverArtist, StringComparison.OrdinalIgnoreCase))
                return PersonRole.CoverArtist;

            if (string.Equals(Contribution, Letterer, StringComparison.OrdinalIgnoreCase))
                return PersonRole.Letterer;

            return null;
        }
    }

}

public sealed record HardcoverTagging
{
    public HardcoverTag Tag { get; init; }
}

public sealed record HardcoverTag: HardcoverEntity
{
    public string Tag { get; init; }
    public HardcoverTagCategory TagCategory { get; init; }
}

public sealed record HardcoverTagCategory
{
    public const string Mood = nameof(Mood);
    public const string Genre = nameof(Genre);
    public const string Pace = nameof(Pace);

    public string Category { get; init; }
}

public sealed record HardcoverImage
{
    public string Url { get; init; }
}
