using System.Text.Json.Serialization;

namespace Mnema.Metadata.Hardcover;

public class HardcoverGetSeriesInfoByIdResponse
{
    [JsonPropertyName("series_by_pk")]
    public HardcoverSeries Series { get; set; }
}

public class HardcoverGetSeriesInfoByIdsResponse
{
    [JsonPropertyName("series")]
    public List<HardcoverSeries> Series { get; set; }
}

public class HardcoverSearchSeriesResponse
{
    [JsonPropertyName("search")]
    public HardcoverPagination<HardcoverSearchSeries> Series { get; set; }
}

public class HardcoverPagination<T>
{

    [JsonPropertyName("results")]
    public HardcoverPaginationResult<T> Results { get; set; }
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("per_page")]
    public int PageSize { get; set; }
}

public class HardcoverPaginationResult<T>
{
    [JsonPropertyName("hits")]
    public List<HardcoverPaginationResultItem<T>> Hits { get; set; }
    [JsonPropertyName("found")]
    public int Found { get; set; }
}

public class HardcoverPaginationResultItem<T>
{
    [JsonPropertyName("document")]
    public T Item { get; set; }
}

public class HardcoverSearchSeries
{
    [JsonPropertyName("author")]
    public HardcoverAuthor Author { get; set; }
    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; }
    [JsonPropertyName("books")]
    public List<string> Books  { get; set; }
    [JsonPropertyName("books_count")]
    public int BooksCount { get; set; }
    [JsonPropertyName("id")]
    public string Id  { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("primary_books_count")]
    public int PrimaryBooksCount { get; set; }
    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}
