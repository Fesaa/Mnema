using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.DTOs.User;

namespace Mnema.Server.Controllers;

public class UserController(ILogger<UserController> logger, IUnitOfWork unitOfWork): BaseApiController
{

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {

        var user = await unitOfWork.UserRepository.GetUserByIdOrDefault(UserId);
        if (user == null) return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Name = UserName, 
            Roles = UserRoles.ToList(),
        });
    }
    
}