using Microsoft.AspNetCore.Mvc;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Server.Controllers;



public class ParserController(IParserService parserService): BaseApiController
{

    [HttpGet]
    public ActionResult<ParseResult> TestParse([FromQuery] string query, [FromQuery] ContentFormat contentFormat)
    {
        var res = parserService.FullParse(query, contentFormat);

        return Ok(res);
    }

}
