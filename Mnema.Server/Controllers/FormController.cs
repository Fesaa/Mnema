using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

public class FormController: BaseApiController
{

    [HttpGet("metadata-provider-settings")]
    [Authorize(Roles.ManageSettings)]
    public ActionResult<FormDefinition> GetMetadataProviderSettings()
    {
        var form = new FormDefinition
        {
            Key = "metadata_provider_settings",
            DescriptionKey = "metadata_provider_settings_description",
            Controls = [
                new FormControlDefinition
                {
                    Key = "priority",
                    Field = "priority",
                    Type = FormType.Text,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMin(0)
                        .Build()
                },
                new FormControlDefinition
                {
                    Key = "enabled",
                    Field = "enabled",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },

                new FormControlDefinition
                {
                    Key = "series_settings_title",
                    Field = "seriesSettings.title",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_summary",
                    Field = "seriesSettings.summary",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_localized_series",
                    Field = "seriesSettings.localizedSeries",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_cover_url",
                    Field = "seriesSettings.coverUrl",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_publication_status",
                    Field = "seriesSettings.publicationStatus",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_year",
                    Field = "seriesSettings.year",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_age_rating",
                    Field = "seriesSettings.ageRating",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_tags",
                    Field = "seriesSettings.tags",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_people",
                    Field = "seriesSettings.people",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_links",
                    Field = "seriesSettings.links",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },
                new FormControlDefinition
                {
                    Key = "series_settings_chapters",
                    Field = "seriesSettings.chapters",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true
                },

                new FormControlDefinition
                {
                    Key = "chapter_settings_title",
                    Field = "seriesSettings.chapterSettings.title",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true,
                    Advanced = true
                },
                new FormControlDefinition
                {
                    Key = "chapter_settings_summary",
                    Field = "seriesSettings.chapterSettings.summary",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true,
                    Advanced = true
                },
                new FormControlDefinition
                {
                    Key = "chapter_settings_cover",
                    Field = "seriesSettings.chapterSettings.cover",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true,
                    Advanced = true
                },
                new FormControlDefinition
                {
                    Key = "chapter_settings_release_date",
                    Field = "seriesSettings.chapterSettings.releaseDate",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true,
                    Advanced = true
                },
                new FormControlDefinition
                {
                    Key = "chapter_settings_people",
                    Field = "seriesSettings.chapterSettings.people",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true,
                    Advanced = true
                },
                new FormControlDefinition
                {
                    Key = "chapter_settings_tags",
                    Field = "seriesSettings.chapterSettings.tags",
                    Type = FormType.Switch,
                    ValueType = FormValueType.Boolean,
                    DefaultOption = true,
                    Advanced = true
                }
            ]
        };

        return Ok(form);
    }

}
