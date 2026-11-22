using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.DTOs;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("overdue-by-user")]
    public async Task<ActionResult<List<OverdueAssigneeDto>>> GetOverdueByUser(CancellationToken cancellationToken) =>
        Ok(await reports.ListOverdueByUserAsync(User, cancellationToken));
}
