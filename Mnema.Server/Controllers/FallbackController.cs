using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Mnema.Server.Controllers;

public class FallbackController: Controller
{
    [Authorize]
    [SwaggerIgnore]
    public PhysicalFileResult Index()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html"), "text/HTML");
    }
}