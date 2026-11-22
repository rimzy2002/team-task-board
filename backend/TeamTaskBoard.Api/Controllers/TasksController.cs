using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.DTOs;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TasksController(TaskService tasks) : ControllerBase
{
    [HttpGet("projects/{id:long}/tasks")]
    public async Task<ActionResult<TaskPageDto>> List(long id, [FromQuery] TaskListQuery filters, CancellationToken cancellationToken)
    {
        var page = await tasks.ListAsync(User, id, filters, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPost("projects/{id:long}/tasks")]
    public async Task<ActionResult<TaskDto>> Create(long id, SaveTaskRequest request, CancellationToken cancellationToken) =>
        SaveResponse(await tasks.CreateAsync(User, id, request, cancellationToken), StatusCodes.Status201Created);

    [HttpPut("tasks/{id:long}")]
    public async Task<ActionResult<TaskDto>> Update(long id, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        SaveResponse(await tasks.UpdateAsync(User, id, request, cancellationToken), StatusCodes.Status200OK);

    [HttpDelete("tasks/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        await tasks.DeleteAsync(User, id, cancellationToken) ? NoContent() : NotFound();

    private ActionResult<TaskDto> SaveResponse(TaskSaveResult result, int successStatus)
    {
        if (result.Status == TaskSaveStatus.NotFound) return NotFound();
        if (result.Status == TaskSaveStatus.PastDueDate)
        {
            ModelState.AddModelError("dueDate", "A new or changed due date must be today or later (UTC).");
            return ValidationProblem(ModelState);
        }

        return StatusCode(successStatus, result.Task);
    }
}
