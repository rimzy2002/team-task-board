using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.DTOs;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProjectsController(ProjectService projects) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await projects.ListAsync(User, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await projects.CreateAsync(User, request, cancellationToken));
}
