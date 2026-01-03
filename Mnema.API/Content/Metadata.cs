using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface IMetadataService
{
    /// <summary>
    /// Processes the input tags for the given preferences and returns the (Genres, Tags)
    /// </summary>
    /// <param name="preferences"></param>
    /// <param name="inputTags"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    (List<string>, List<string>) ProcessTags(UserPreferences preferences, IList<Tag> inputTags, DownloadRequestDto request);

    /// <summary>
    /// Given the input tags, returns the highest mapped age rating
    /// </summary>
    /// <param name="preferences"></param>
    /// <param name="inputTags"></param>
    /// <returns></returns>
    AgeRating? GetAgeRating(UserPreferences preferences, IList<Tag> inputTags);

    /// <summary>
    /// Transforms the tags, as defined by the mappings
    /// </summary>
    /// <param name="tags"></param>
    /// <param name="mappings"></param>
    /// <returns></returns>
    List<Tag> MapTags(IList<Tag> tags, IList<TagMappingDto> mappings);
}