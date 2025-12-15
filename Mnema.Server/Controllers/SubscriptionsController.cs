using Microsoft.AspNetCore.Authorization;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

using Microsoft.AspNetCore.Mvc;

[Authorize(Roles.Subscriptions)]
public class SubscriptionsController(ILogger<SubscriptionsController> logger, IUnitOfWork unitOfWork, ISubscriptionService subscriptionService): BaseApiController
{

    [HttpGet("providers")]
    public async Task<ActionResult<IList<Provider>>> GetProviders()
    {
        return Ok(ISubscriptionService.SubscriptionProviders);
    }

    [HttpGet("all")]
    public async Task<ActionResult<PagedList<SubscriptionDto>>> GetAllSubscriptions([FromQuery] PaginationParams? paginationParams)
    {
        paginationParams ??= PaginationParams.Default;
        
        return Ok(await unitOfWork.SubscriptionRepository.GetSubscriptionDtosForUser(UserId, paginationParams));
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
        throw new NotImplementedException();
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

    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll([FromQuery] bool allUsers = false)
    {
        throw new NotImplementedException();
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
    
}