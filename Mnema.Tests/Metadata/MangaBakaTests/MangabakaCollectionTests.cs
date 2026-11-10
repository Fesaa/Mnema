using System;
using System.Collections.Generic;
using System.Text.Json;
using Mnema.Common.Extensions;
using Mnema.Metadata.Mangabaka;
using Mnema.Models.Publication;

namespace Mnema.Tests.Metadata.MangaBakaTests;

public class MangabakaCollectionTests
{
    private static MangabakaCollection Collection(string id, string type, string medium, string language,
        int countMain = 16)
    {
        return new MangabakaCollection
        {
            Id = id,
            Type = type,
            Medium = medium,
            CountMain = countMain,
            Language = new MangabakaCollection.LanguageInfo { Iso = language },
        };
    }

    private static MangabakaWork Work(string id, string? sequence = null, string? partOfVolume = null)
    {
        return new MangabakaWork
        {
            Id = id,
            SequenceString = sequence,
            SequenceNumeric = sequence != null && float.TryParse(sequence, out var numeric) ? numeric : null,
            PartOfVolume = partOfVolume,
        };
    }

    [Fact]
    public void SelectCollection_PreferPhysical_PicksPaperbackOverDigital()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("digital", "volume", "digital", "en"),
            Collection("paperback", "volume", "paperback", "en"),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferPhysical, "ja");

        Assert.NotNull(result);
        Assert.Equal("paperback", result.Id);
    }

    [Fact]
    public void SelectCollection_PreferPhysical_PicksCollectionWithMostWorks()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("short-paperback", "volume", "paperback", "en", 4),
            Collection("digital", "volume", "digital", "en", 16),
            Collection("paperback", "volume", "paperback", "en", 16),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferPhysical, "ja");

        Assert.NotNull(result);
        Assert.Equal("paperback", result.Id);
    }

    [Fact]
    public void SelectCollection_PreferDigital_PicksDigital()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("paperback", "volume", "paperback", "en"),
            Collection("digital", "volume", "digital", "en"),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferDigital, "ja");

        Assert.NotNull(result);
        Assert.Equal("digital", result.Id);
    }

    [Fact]
    public void SelectCollection_PreferPhysical_FallsBackToDigital()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("digital", "volume", "digital", "en"),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferPhysical, "ja");

        Assert.NotNull(result);
        Assert.Equal("digital", result.Id);
    }

    [Fact]
    public void SelectCollection_PrefersVolumeCollectionOverChapterCollection()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("chapters", "chapter", "digital", "en"),
            Collection("volumes", "volume", "digital", "en"),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferDigital, "ja");

        Assert.NotNull(result);
        Assert.Equal("volumes", result.Id);
    }

    [Fact]
    public void SelectCollection_PrefersEnglishLanguage()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("french", "volume", "paperback", "fr"),
            Collection("english", "volume", "paperback", "en"),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferPhysical, "ja");

        Assert.NotNull(result);
        Assert.Equal("english", result.Id);
    }

    [Fact]
    public void SelectCollection_SkipsCollectionsWithoutWorks()
    {
        var collections = new List<MangabakaCollection>
        {
            Collection("empty", "volume", "paperback", "en", 0),
        };

        var result = MangabakaMetadataService.SelectCollection(collections,
            MangabakaCollectionMedium.PreferPhysical, "ja");

        Assert.Null(result);
    }

    [Fact]
    public void ToChapter_MapsVolumeCollection()
    {
        var collection = Collection("paperback", "volume", "paperback", "en");
        var work = new MangabakaWork
        {
            Id = "spice-and-wolf-volume-1",
            SubTitle = "Spice and Wolf, Vol. 1",
            SequenceString = "1",
            SequenceNumeric = 1,
            ReleaseDate = new DateOnly(2010, 4, 20),
            Description = new MangabakaWork.DescriptionInfo
            {
                Desc = "Lawrence meets Holo, the Wolf-God of the harvest."
            },
            Identifiers =
            [
                new MangabakaWork.IdentifierInfo { Id = "9780316073394", Name = "isbn" },
            ],
        };

        var chapter = MangabakaMetadataService.ToChapter(work, collection);

        Assert.Equal("spice-and-wolf-volume-1", chapter.Id);
        Assert.Equal("Spice and Wolf, Vol. 1", chapter.Title);
        Assert.Equal("Lawrence meets Holo, the Wolf-God of the harvest.", chapter.Summary);
        Assert.Equal("1", chapter.VolumeMarker);
        Assert.Equal(string.Empty, chapter.ChapterMarker);
        Assert.Equal(1f, chapter.SortOrder);
        Assert.Equal("9780316073394", chapter.Isbn);
        Assert.Equal(new DateTime(2010, 4, 20, 0, 0, 0, DateTimeKind.Utc), chapter.ReleaseDate);
    }

    [Fact]
    public void ToChapter_MapsChapterCollection()
    {
        var collection = Collection("chapters", "chapter", "digital", "en");
        var work = Work("spice-and-wolf-chapter-5", sequence: "5", partOfVolume: "2");

        var chapter = MangabakaMetadataService.ToChapter(work, collection);

        Assert.Equal("2", chapter.VolumeMarker);
        Assert.Equal("5", chapter.ChapterMarker);
    }

    [Fact]
    public void ToChapter_MapsOneShotCollection()
    {
        var collection = Collection("one-shot", "one-shot", "digital", "en");
        var work = Work("spice-and-wolf-one-shot", sequence: "1");

        var chapter = MangabakaMetadataService.ToChapter(work, collection);

        Assert.Equal(string.Empty, chapter.VolumeMarker);
        Assert.Equal(string.Empty, chapter.ChapterMarker);
    }

    [Fact]
    public void ToChapter_UsesWorkImageAsCover()
    {
        var collection = Collection("paperback", "volume", "paperback", "en");
        var work = new MangabakaWork
        {
            Id = "spice-and-wolf-volume-1",
            Images =
            [
                new MangabakaCover
                {
                    Type = "volume",
                    Image = new MangabakaCoverImage
                    {
                        RawImage = new MangabakaCoverRawImage
                        {
                            Url = "https://images.mangabaka.dev/spice-and-wolf-1",
                            Format = "jpeg",
                        }
                    }
                }
            ],
        };

        var chapter = MangabakaMetadataService.ToChapter(work, collection);

        Assert.Equal("https://images.mangabaka.dev/spice-and-wolf-1", chapter.CoverUrl);
        Assert.Equal(".jpeg", chapter.CoverFileFormat);
    }

    [Fact]
    public void ToChapter_HandlesMissingOptionalFields()
    {
        var collection = Collection("paperback", "volume", "paperback", "en");
        var work = new MangabakaWork { Id = "spice-and-wolf-volume-1" };

        var chapter = MangabakaMetadataService.ToChapter(work, collection);

        Assert.Equal(string.Empty, chapter.Title);
        Assert.Equal(string.Empty, chapter.Summary);
        Assert.Equal(string.Empty, chapter.VolumeMarker);
        Assert.Equal(string.Empty, chapter.ChapterMarker);
        Assert.Null(chapter.ReleaseDate);
        Assert.Null(chapter.CoverUrl);
        Assert.Null(chapter.CoverFileFormat);
        Assert.Null(chapter.Isbn);
        Assert.Empty(chapter.Tags);
        Assert.Empty(chapter.People);
        Assert.Empty(chapter.TranslationGroups);
    }

    [Fact]
    public void DeserializeCollections_MapsSpiceAndWolfPayload()
    {
        const string json = """
        {
          "status": 200,
          "data": [
            {
              "id": "019dcac7-c4a2-79ed-86da-a14dcfd3bc72",
              "series_id": 10803,
              "title": "Spice & Wolf",
              "type": "volume",
              "medium": "digital",
              "count_main": 16,
              "count_extra": 0,
              "count_other": 0,
              "language": { "iso": "en", "language": "English (English)" }
            },
            {
              "id": "019dcac7-c481-71ad-9802-c145d9edca96",
              "series_id": 10803,
              "title": "Spice & Wolf",
              "type": "volume",
              "medium": "paperback",
              "count_main": 16,
              "count_extra": 0,
              "count_other": 0,
              "language": { "iso": "en", "language": "English (English)" }
            }
          ],
          "pagination": { "count": 2, "limit": 50, "next": null }
        }
        """;

        var response = JsonSerializer.Deserialize<PaginatedResponse<MangabakaCollection>>(json,
            HttpClientExtensions.JsonSerializerOptions);

        Assert.NotNull(response);
        Assert.Equal(2, response.Data!.Count);
        Assert.Equal("en", response.Data[1].Language!.Iso);

        var selected = MangabakaMetadataService.SelectCollection(response.Data,
            MangabakaCollectionMedium.PreferPhysical, "ja");

        Assert.NotNull(selected);
        Assert.Equal("019dcac7-c481-71ad-9802-c145d9edca96", selected.Id);
    }

    [Fact]
    public void DeserializeCollectionFull_MapsSpiceAndWolfWork()
    {
        const string json = """
        {
          "status": 200,
          "data": {
            "type": "volume",
            "medium": "paperback",
            "works": [
              {
                "id": "019dcaf9-71c3-7eea-bb79-000fce9f63db",
                "sequence_string": "1",
                "sequence_numeric": 1,
                "part_of_volume": null,
                "count_type": "main",
                "release_date": "2010-04-20",
                "description": { "desc": "Lawrence meets Holo, the Wolf-God of the harvest." },
                "identifiers": [ { "id": "9780316073394", "name": "isbn" } ],
                "images": []
              }
            ]
          }
        }
        """;

        var response = JsonSerializer.Deserialize<MangabakaCollectionFullResponse>(json,
            HttpClientExtensions.JsonSerializerOptions);

        Assert.NotNull(response);
        var work = Assert.Single(response.Data!.Works!);
        var collection = Collection("paperback", "volume", "paperback", "en");
        var chapter = MangabakaMetadataService.ToChapter(work, collection);

        Assert.Equal("019dcaf9-71c3-7eea-bb79-000fce9f63db", chapter.Id);
        Assert.Equal("1", chapter.VolumeMarker);
        Assert.Equal(string.Empty, chapter.ChapterMarker);
        Assert.Equal("9780316073394", chapter.Isbn);
        Assert.Equal(new DateTime(2010, 4, 20, 0, 0, 0, DateTimeKind.Utc), chapter.ReleaseDate);
    }
}
