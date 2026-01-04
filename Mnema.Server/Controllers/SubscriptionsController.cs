using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using ValueType = Mnema.Models.DTOs.UI.ValueType;

namespace Mnema.Server.Controllers;

[Authorize(Roles.Subscriptions)]
public class SubscriptionsController(
    ILogger<SubscriptionsController> logger,
    IUnitOfWork unitOfWork,
    ISubscriptionService subscriptionService,
    IServiceProvider serviceProvider
) : BaseApiController
{
    [HttpGet("providers")]
    public ActionResult<IList<Provider>> GetProviders()
    {
        return Ok(ISubscriptionService.SubscriptionProviders);
    }

    [HttpGet("all")]
    public async Task<ActionResult<PagedList<SubscriptionDto>>> GetAllSubscriptions([FromQuery] string query = "",
        [FromQuery] PaginationParams? paginationParams = null)
    {
        paginationParams ??= PaginationParams.Default;

        return Ok(await unitOfWork.SubscriptionRepository.GetSubscriptionDtosForUser(UserId, query, paginationParams));
    }

    [HttpGet("{subscriptionId:guid}")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(Guid subscriptionId)
    {
        var sub = await unitOfWork.SubscriptionRepository.GetSubscriptionDto(subscriptionId);
        if (sub == null) return NotFound();

        if (sub.UserId != UserId) return Forbid();

        return Ok(sub);
    }

    [HttpPost("run-once/{subscriptionId:guid}")]
    public async Task<IActionResult> RunOnce(Guid subscriptionId)
    {
        await subscriptionService.RunOnce(UserId, subscriptionId);

        return Ok();
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateSubscription([FromBody] SubscriptionDto updateDto)
    {
        await subscriptionService.UpdateSubscription(UserId, updateDto);

        return Ok();
    }

    [HttpPost("new")]
    public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionDto createDto)
    {
        await subscriptionService.CreateSubscription(UserId, createDto);

        return Ok();
    }

    [HttpDelete("{subscriptionId:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid subscriptionId)
    {
        var sub = await unitOfWork.SubscriptionRepository.GetSubscription(subscriptionId);
        if (sub == null) return NotFound();

        if (sub.UserId != UserId) return Forbid();

        unitOfWork.SubscriptionRepository.Delete(sub);

        await unitOfWork.CommitAsync();

        return Ok();
    }

    [HttpGet("form")]
    public ActionResult<FormDefinition> GetForm()
    {
        return Ok(new FormDefinition
        {
            Key = "edit-subscription-modal",
            Controls = [
                new FormControlDefinition
                {
                    Key = "title",
                    Field = "title",
                    Type = FormType.Text,
                    ForceSingle = true,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "content-id",
                    Field = "contentId",
                    Type = FormType.Text,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "base-dir",
                    Field = "baseDir",
                    Type = FormType.Directory,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "provider",
                    Field = "provider",
                    Type = FormType.DropDown,
                    ValueType = ValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = ISubscriptionService.SubscriptionProviders
                        .Select(provider => new FormControlOption(provider.ToString().ToLower(), provider))
                        .ToList(),
                },
                new FormControlDefinition
                {
                    Key = "refresh-frequency",
                    Field = "refreshFrequency",
                    Type = FormType.DropDown,
                    ValueType = ValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    DefaultOption = RefreshFrequency.Week,
                    Options = Enum.GetValues<RefreshFrequency>()
                        .Select(rf => new FormControlOption(rf.ToString().ToLower(), rf))
                        .ToList(),
                },
            ]
        });
    }
}
