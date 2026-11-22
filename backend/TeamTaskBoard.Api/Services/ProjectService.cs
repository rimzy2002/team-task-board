using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.DTOs;

namespace TeamTaskBoard.Api.Services;

public sealed class ProjectService(TeamTaskBoardContext db, LocalUserService localUsers, TimeProvider clock)
{
    public async Task<List<ProjectDto>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        return await db.Projects.AsNoTracking()
            .Where(project => project.OwnerId == owner.Id)
            .OrderByDescending(project => project.Id)
            .Select(project => new ProjectDto(project.Id, project.Name, project.Description,
                DateTime.SpecifyKind(project.CreatedAtUtc, DateTimeKind.Utc),
                DateTime.SpecifyKind(project.UpdatedAtUtc, DateTimeKind.Utc)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(ClaimsPrincipal principal, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        // MySQL DATETIME(6) stores microseconds. Return the same precision on create and list.
        var now = clock.GetUtcNow().UtcDateTime;
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
        var project = new Project
        {
            OwnerId = owner.Id,
            Name = request.Name!.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.UpdatedAtUtc);
    }
}
