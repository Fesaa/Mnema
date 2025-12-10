using Mnema.API.Services;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Server.Controllers;

using Microsoft.AspNetCore.Mvc;

public class SubscriptionsController: BaseApiController
{

    [HttpGet("providers")]
    public async Task<ActionResult<IList<Provider>>> GetProviders()
    {
        return Ok(ISubscriptionService.SubscriptionProviders);
    }

    [HttpGet("all")]
    public async Task<ActionResult<IList<SubscriptionDto>>> GetAllSubscriptions([FromQuery] bool allUsers = false)
    {
        return Ok(new List<SubscriptionDto>());
    }

    [HttpGet("{subscriptionId:guid}")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(Guid subscriptionId)
    {
        throw new NotImplementedException();
    }

    [HttpPost("run-once/{subscriptionId:guid}")]
    public async Task<IActionResult> RunOnce(Guid subscriptionId)
    {
        throw new NotImplementedException();
    }

    /*[HttpPost("update")]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdateSubscriptionDto updateDto)
    {
        throw new NotImplementedException();
    }

    [HttpPost("new")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto createDto)
    {
        throw new NotImplementedException();
    }*/

    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll([FromQuery] bool allUsers = false)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{subscriptionId:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid subscriptionId)
    {
        throw new NotImplementedException();
    }
    
}