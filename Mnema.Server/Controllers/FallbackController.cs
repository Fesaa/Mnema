using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Mnema.Server.Controllers;

public class FallbackController: Controller
{
    [Authorize]
    [SwaggerIgnore]
    public IActionResult Index()
    {
        if (HttpContext.Request.Path.StartsWithSegments("/api") || HttpContext.Request.Path.StartsWithSegments("/ws"))
        {
            return NotFound();
        }
        
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html"), "text/HTML");
    }
}