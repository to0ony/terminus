using Microsoft.AspNetCore.Mvc;
using Terminus.Service.Common;

namespace Terminus.WebApi.Controllers;

[ApiController]
[Route("api/gtfs")]
public class GtfsController(IGtfsExportService gtfsExportService) : ControllerBase
{
    /// <summary>
    /// GTFS feed kao ZIP. Vraća 422 s popisom problema ako podaci
    /// nisu spremni za izvoz.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/zip")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var feed = await gtfsExportService.ExportAsync(cancellationToken);
        return File(feed.Content, "application/zip", feed.FileName);
    }
}
