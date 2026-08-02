using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

public class FormController(IProviderSettingsService providerSettingsService): BaseApiController
{

    [Authorize(Roles.ManageSettings)]
    [HttpGet("metadata-provider-settings")]
    public ActionResult<FormDefinition> GetMetadataProviderSettings()
    {
        var form = new FormDefinition
        {
            Key = "metadata_provider_settings",
            DescriptionKey = "metadata_provider_settings_description",
            Controls = [
                new IntegerFieldDefinition
                {
                    Key = "priority",
                    Field = "priority",
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMin(0)
                        .Build()
                },
                new SwitchFieldDefinition
                {
                    Key = "enabled",
                    Field = "enabled",
                    DefaultValue = true,
                },

                new SwitchFieldDefinition
                {
                    Key = "series_settings_title",
                    Field = "seriesSettings.title",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_summary",
                    Field = "seriesSettings.summary",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_localized_series",
                    Field = "seriesSettings.localizedSeries",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_cover_url",
                    Field = "seriesSettings.coverUrl",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_publication_status",
                    Field = "seriesSettings.publicationStatus",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_year",
                    Field = "seriesSettings.year",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_age_rating",
                    Field = "seriesSettings.ageRating",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_tags",
                    Field = "seriesSettings.tags",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_people",
                    Field = "seriesSettings.people",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_links",
                    Field = "seriesSettings.links",
                    DefaultValue = true,
                },
                new SwitchFieldDefinition
                {
                    Key = "series_settings_chapters",
                    Field = "seriesSettings.chapters",
                    DefaultValue = true,
                },

                new SwitchFieldDefinition
                {
                    Key = "chapter_settings_title",
                    Field = "seriesSettings.chapterSettings.title",
                    DefaultValue = true,
                    Advanced = true
                },
                new SwitchFieldDefinition
                {
                    Key = "chapter_settings_summary",
                    Field = "seriesSettings.chapterSettings.summary",
                    DefaultValue = true,
                    Advanced = true
                },
                new SwitchFieldDefinition
                {
                    Key = "chapter_settings_cover",
                    Field = "seriesSettings.chapterSettings.cover",
                    DefaultValue = true,
                    Advanced = true
                },
                new SwitchFieldDefinition
                {
                    Key = "chapter_settings_release_date",
                    Field = "seriesSettings.chapterSettings.releaseDate",
                    DefaultValue = true,
                    Advanced = true
                },
                new SwitchFieldDefinition
                {
                    Key = "chapter_settings_people",
                    Field = "seriesSettings.chapterSettings.people",
                    DefaultValue = true,
                    Advanced = true
                },
                new SwitchFieldDefinition
                {
                    Key = "chapter_settings_tags",
                    Field = "seriesSettings.chapterSettings.tags",
                    DefaultValue = true,
                    Advanced = true
                }
            ]
        };

        return Ok(form);
    }

    [Authorize(Roles.ManageSettings)]
    [HttpGet("provider-settings")]
    public async Task<ActionResult<FormDefinition>> GetProviderSettingsForms([FromQuery] Provider provider)
    {
        return Ok(await providerSettingsService.GetSettingsForm(provider, HttpContext.RequestAborted));
    }

    [HttpGet("preferences")]
    [Authorize(Roles.ManageSettings)]
    public ActionResult<FormDefinition> GetPreferencesForm()
    {
        return Ok(new FormDefinition
        {
            Key = "settings.preferences",
            Controls = [
                FormFieldDefinitions.EnumDropDown<ImageFormat>(nameof(Preferences.ImageFormat).ToCamelCase(), "image-format-pipe"),
                FormFieldDefinitions.EnumDropDown<CoverFallbackMethod>(nameof(Preferences.CoverFallbackMethod).ToCamelCase(), "cover-fallback-method-pipe"),
                new CommaSeparatedValuesFieldDefinition
                {
                    Field = nameof(Preferences.BlackListedTags).ToCamelCase(),
                    ForceSingle = true,
                },
                new CommaSeparatedValuesFieldDefinition
                {
                    Field = nameof(Preferences.WhiteListedTags).ToCamelCase(),
                    ForceSingle = true,
                },
                new ArrayFieldDefinition
                {
                    Field = nameof(Preferences.AgeRatingMappings).ToCamelCase(),
                    Controls = [
                        new TextFieldDefinition { Field = nameof(AgeRatingMappingDto.Tag).ToCamelCase(), ForceEditMode = true, Validators = FormValidatorsBuilder.Required},
                        FormFieldDefinitions.EnumDropDown<AgeRating>(nameof(AgeRatingMappingDto.AgeRating).ToCamelCase(), "age-rating-pipe")
                    ]
                },
                new ArrayFieldDefinition
                {
                    Field = nameof(Preferences.MetadataFieldMappings).ToCamelCase(),
                    Inline = true,
                    Controls = [
                        FormFieldDefinitions.EnumDropDown<MetadataFieldType>(nameof(MetadataFieldMappingDto.SourceType).ToCamelCase(), "metadata-field-type-pipe"),
                        new TextFieldDefinition { Field = nameof(MetadataFieldMappingDto.SourceValue).ToCamelCase(), ForceEditMode = true, Validators = FormValidatorsBuilder.Required },
                        FormFieldDefinitions.EnumDropDown<MetadataFieldType>(nameof(MetadataFieldMappingDto.DestinationType).ToCamelCase(), "metadata-field-type-pipe"),
                        new TextFieldDefinition { Field = nameof(MetadataFieldMappingDto.DestinationValue).ToCamelCase(), ForceEditMode = true, Validators = FormValidatorsBuilder.Required },
                        new SwitchFieldDefinition { Field = nameof(MetadataFieldMappingDto.ExcludeFromSource).ToCamelCase(), ForceEditMode = true, Validators = FormValidatorsBuilder.Required }
                    ],
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(Preferences.PinSubscriptionTitles).ToCamelCase(),
                }
            ]
        });
    }

}
