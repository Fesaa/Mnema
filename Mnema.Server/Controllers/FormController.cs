using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

public class FormController(IProviderSettingsService providerSettingsService, IServiceScopeFactory scopeFactory): BaseApiController
{

    [Authorize(Roles.ManageSettings)]
    [HttpGet("metadata-provider-settings")]
    public async Task<ActionResult<FormDefinition>> GetMetadataProviderSettings([FromQuery] MetadataProvider metadataProvider)
    {
        using var scope = scopeFactory.CreateScope();
        var configurationService = scope.ServiceProvider.GetKeyedService<IConfigurationProvider>(metadataProvider);

        List<FormFieldDefinition> specificFormControls = [];
        if (configurationService is not null)
            specificFormControls = await configurationService.GetFormControls(HttpContext.RequestAborted);

        return Ok(new FormDefinition
        {
            Key = "settings.metadata-provider",
            Controls = [
                new IntegerFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.Priority).ToCamelCase(),
                    Hidden = true,
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.Enabled).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesTitle).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesAgeRating).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesLocalizedName).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesCoverUrl).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesPublicationStatus).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesAgeRating).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesYear).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesTags).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesPeople).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.SeriesLinks).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.Chapters).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.ChapterTitle).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.ChapterSummary).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.ChapterReleaseDate).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.ChapterPeople).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.ChapterTags).ToCamelCase()
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(MetadataProviderSettings.ChapterCoverUrl).ToCamelCase()
                },
                ..specificFormControls
            ]
        });
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
        Controls =
        [
            new TextFieldDefinition
            {
                Field = nameof(Preferences.ChapterFileFormat).ToCamelCase(),
                DefaultValue = INamingService.DefaultChapterFormat,
                ForceSingle = true,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithServerSideValidation("Preferences/valid-chapter-format")
                    .Build(),
                WikiLink = WikiLinks.NamingFormatDocumentation,
            },
            new TextFieldDefinition
            {
                Field = nameof(Preferences.OneShotFileFormat).ToCamelCase(),
                DefaultValue = INamingService.DefaultOneShotFormat,
                ForceSingle = true,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithServerSideValidation("Preferences/valid-one-shot-format")
                    .Build(),
                WikiLink = WikiLinks.NamingFormatDocumentation,
            },
            FormFieldDefinitions.EnumDropDown<ImageFormat>(
                nameof(Preferences.ImageFormat).ToCamelCase(),
                "image-format-pipe"),
            FormFieldDefinitions.EnumDropDown<CoverFallbackMethod>(
                nameof(Preferences.CoverFallbackMethod).ToCamelCase(),
                "cover-fallback-method-pipe"),
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
                Controls =
                [
                    new TextFieldDefinition
                    {
                        Field = nameof(AgeRatingMappingDto.Tag).ToCamelCase(),
                        ForceEditMode = true,
                        Validators = FormValidatorsBuilder.Required,
                        HideText = true,
                    },
                    FormFieldDefinitions.EnumDropDown<AgeRating>(
                        nameof(AgeRatingMappingDto.AgeRating).ToCamelCase(),
                        "age-rating-pipe") with { HideText = true },
                ],
            },
            new ArrayFieldDefinition
            {
                Field = nameof(Preferences.MetadataFieldMappings).ToCamelCase(),
                Inline = true,
                Controls =
                [
                    FormFieldDefinitions.EnumDropDown<MetadataFieldType>(
                        nameof(MetadataFieldMappingDto.SourceType).ToCamelCase(),
                        "metadata-field-type-pipe") with { HideText = true },
                    new TextFieldDefinition
                    {
                        Field = nameof(MetadataFieldMappingDto.SourceValue).ToCamelCase(),
                        ForceEditMode = true,
                        Validators = FormValidatorsBuilder.Required,
                        HideText = true,
                    },
                    FormFieldDefinitions.EnumDropDown<MetadataFieldType>(
                        nameof(MetadataFieldMappingDto.DestinationType).ToCamelCase(),
                        "metadata-field-type-pipe") with { HideText = true },
                    new TextFieldDefinition
                    {
                        Field = nameof(MetadataFieldMappingDto.DestinationValue).ToCamelCase(),
                        ForceEditMode = true,
                        Validators = FormValidatorsBuilder.Required,
                        HideText = true,
                    },
                    new SwitchFieldDefinition
                    {
                        Field = nameof(MetadataFieldMappingDto.ExcludeFromSource).ToCamelCase(),
                        ForceEditMode = true,
                        Validators = FormValidatorsBuilder.Required,
                        HideText = true,
                    },
                ],
            },
            new SwitchFieldDefinition
            {
                Field = nameof(Preferences.PinSubscriptionTitles).ToCamelCase(),
            },
        ],
    });
}

    [HttpGet("server-settings")]
    [Authorize(Roles.ManageSettings)]
    public ActionResult<FormDefinition> GetServerSettingsForm()
    {
        return Ok(new FormDefinition
        {
            Key = "settings.server",
            Controls = [
                new IntegerFieldDefinition
                {
                    Field = nameof(UpdateServerSettingsDto.MaxConcurrentImages).ToCamelCase(),
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMin(1)
                        .WithMax(5)
                        .Build(),
                    ForceSingle = true,
                },
                new IntegerFieldDefinition
                {
                    Field = nameof(UpdateServerSettingsDto.AutoDisableProviderAfter).ToCamelCase(),
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMin(0)
                        .Build(),
                    ForceSingle = true,
                },
                new SwitchFieldDefinition
                {
                    Field = nameof(UpdateServerSettingsDto.ImageConversionLossless).ToCamelCase(),
                    Validators = FormValidatorsBuilder.Required,
                    ForceSingle = true,
                },
                new IntegerFieldDefinition
                {
                    Field = nameof(UpdateServerSettingsDto.ImageConversionQuality).ToCamelCase(),
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMin(0)
                        .WithMax(100)
                        .Build(),
                    ForceSingle = true,
                }
            ]
        });
    }

}
