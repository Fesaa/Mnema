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
    public async Task<IActionResult> UpdateSubscription([FromBody] CreateOrUpdateSubscriptionDto updateDto)
    {
        await subscriptionService.UpdateSubscription(UserId, updateDto);

        return Ok();
    }

    [HttpPost("new")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateOrUpdateSubscriptionDto createDto)
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
        return Ok(subscriptionService.GetForm());
    }
}
