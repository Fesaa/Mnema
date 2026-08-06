using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Metadata;
using Mnema.API.Services;
using Mnema.Common.Exceptions;
using Mnema.Metadata.Mangabaka;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Server.Middleware;

namespace Mnema.Server.Controllers;

public class SettingsController(
    ILogger<SettingsController> logger, ISettingsService settingsService,
    IUnitOfWork unitOfWork, IPasswordService passwordService,
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IMangabakaMetadataService mangabakaMetadataService
    ) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ServerSettingsDto>> GetSettings()
    {
        var dto = await settingsService.GetSettingsAsync();

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles.ManageSettings)]
    public async Task<ActionResult<ServerSettingsDto>> UpdateSettings([FromBody] UpdateServerSettingsDto dto)
    {
        await settingsService.SaveSettingsAsync(dto);

        var settings = await settingsService.GetSettingsAsync();
        return Ok(settings);
    }

    [Authorize(Roles.ManageSettings)]
    [HttpGet("metadata-provider-settings")]
    public async Task<ActionResult<MetadataProviderSettingsV2Dto>> GetMetadataProviderSettings(
        [FromQuery] MetadataProvider metadataProvider)
    {
        var settings = await unitOfWork.MetadataProviderSettingsRepository.GetMetadataProviderSettingsDto(metadataProvider, HttpContext.RequestAborted);

        return Ok(settings);
    }

    [Authorize(Roles.ManageSettings)]
    [HttpPost("metadata-provider-settings")]
    public async Task<ActionResult> UpdateMetadataProviderSettings([FromBody] MetadataProviderSettingsV2Dto dto)
    {
        var settings = await unitOfWork.MetadataProviderSettingsRepository.GetMetadataProviderSettings(dto.MetadataProvider, HttpContext.RequestAborted);

        settings.Enabled = dto.Enabled;

        settings.SeriesTitle = dto.SeriesTitle;
        settings.SeriesSummary = dto.SeriesSummary;
        settings.SeriesLocalizedName = dto.SeriesLocalizedName;
        settings.SeriesCoverUrl = dto.SeriesCoverUrl;
        settings.SeriesPublicationStatus = dto.SeriesPublicationStatus;
        settings.SeriesAgeRating = dto.SeriesAgeRating;
        settings.SeriesYear = dto.SeriesYear;
        settings.SeriesTags = dto.SeriesTags;
        settings.SeriesPeople = dto.SeriesPeople;
        settings.SeriesLinks = dto.SeriesLinks;

        settings.Chapters = dto.Chapters;
        settings.ChapterTitle = dto.ChapterTitle;
        settings.ChapterSummary = dto.ChapterSummary;
        settings.ChapterReleaseDate = dto.ChapterReleaseDate;
        settings.ChapterPeople = dto.ChapterPeople;
        settings.ChapterTags = dto.ChapterTags;
        settings.ChapterCoverUrl = dto.ChapterCoverUrl;

        settings.MetadataProviderSpecific = dto.MetadataProviderSpecific;

        await unitOfWork.CommitAsync(HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPost("validate-link-filter")]
    public ActionResult IsLinkFilterValueValid([FromBody] FormFieldValidationRequestDto<string, LinkFilter> validationRequest)
    {
        if (validationRequest.GroupValue?.Type == LinkFilterType.Language)
        {
            var errors = mangabakaMetadataService.NativeLanguageFormatter.Validate(validationRequest.FormValue);
            if (errors.Count == 0) return Ok(null);

            return Ok(new
            {
                invalidFormat = errors
            });
        }

        return Ok(null);
    }

    [HttpPost("validate-language-format")]
    public ActionResult IsLanguageFormatValid([FromBody] FormFieldValidationRequestDto<List<string>> validationRequest)
    {
        foreach (var language in validationRequest.FormValue)
        {
            var errors = mangabakaMetadataService.NativeLanguageFormatter.Validate(language);
            if (errors.Count > 0)
            {
                return Ok(new
                {
                    invalidFormat = errors
                });
            }
        }

        return Ok(null);
    }

    [Authorize(Roles.ManageSettings)]
    [HttpPost("sort-metadata-providers")]
    public async Task<ActionResult> SortMetadataProviders([FromBody] MetadataProvider[] metadataProviders)
    {
        var allSettings = await unitOfWork.MetadataProviderSettingsRepository.GetAll();

        foreach (var setting in allSettings)
        {
            var index = metadataProviders.IndexOf(setting.MetadataProvider);
            if (index != -1)
            {
                setting.Priority = index;
                continue;
            }

            throw new BadRequestException("Not all providers are present");
        }

        await unitOfWork.CommitAsync(HttpContext.RequestAborted);

        return Ok();
    }

    [Authorize(Roles.ManageSettings)]
    [HttpGet("metadata-provider-order")]
    public async Task<ActionResult<List<MetadataProvider>>> GetMetadataProviderOrder()
    {
        return Ok(await unitOfWork.MetadataProviderSettingsRepository.GetOrder(HttpContext.RequestAborted));
    }

    [HttpGet("is-setup")]
    [AllowAnonymous]
    public async Task<bool> IsSetup()
    {
        var noAuthScheme = await authenticationSchemeProvider.GetSchemeAsync(NoAuthAuthenticationSchemeOptions.SchemeName);
        if (noAuthScheme != null) return true;

        var passwordSetting = await unitOfWork.SettingsRepository.GetSettingsAsync(ServerSettingKey.Password);
        return !string.IsNullOrEmpty(passwordSetting.Value);
    }

    [AllowAnonymous]
    [HttpGet("is-authenticated")]
    public bool IsAuthenticated()
    {
        return User.Identity?.IsAuthenticated ?? false;
    }

    [AllowAnonymous]
    [HttpPost("set-password")]
    public async Task<ActionResult> SetPassword()
    {
        var form = await HttpContext.Request.ReadFormAsync();
        if (!form.TryGetValue("password", out var passwordValues))
        {
            return BadRequest();
        }

        var password = passwordValues.FirstOrDefault();
        if (string.IsNullOrEmpty(password)) return BadRequest();

        var passwordSetting = await unitOfWork.SettingsRepository.GetSettingsAsync(ServerSettingKey.Password);
        if (!string.IsNullOrEmpty(passwordSetting.Value)) return BadRequest();

        passwordSetting.Value = passwordService.HashPassword(password);
        unitOfWork.SettingsRepository.Update(passwordSetting);
        await unitOfWork.CommitAsync();

        return Redirect("/");
    }
}
