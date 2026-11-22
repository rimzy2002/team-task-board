using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.DTOs;

namespace TeamTaskBoard.Api.Services;

public sealed class TaskService(TeamTaskBoardContext db, LocalUserService localUsers, TimeProvider clock)
{
    public async Task<TaskPageDto?> ListAsync(ClaimsPrincipal principal, long projectId, TaskListQuery filters,
        CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        if (!await OwnsProjectAsync(projectId, owner.Id, cancellationToken)) return null;

        var query = OwnedTasks(owner.Id).AsNoTracking().Where(task => task.ProjectId == projectId);
        if (filters.Status is not null)
        {
            var status = Enum.Parse<BoardTaskStatus>(filters.Status);
            query = query.Where(task => task.Status == status);
        }

        if (filters.Priority is not null)
        {
            var priority = Enum.Parse<TaskPriority>(filters.Priority);
            query = query.Where(task => task.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(task => task.Title.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (filters.Page - 1) * TaskListQuery.PageSize;
        List<BoardTask> tasks = offset >= totalCount ? [] : await query.OrderByDescending(task => task.Id)
            .Skip(offset).Take(TaskListQuery.PageSize).ToListAsync(cancellationToken);
        return new TaskPageDto(tasks.Select(ToDto).ToList(), filters.Page, TaskListQuery.PageSize, totalCount);
    }

    public async Task<TaskSaveResult> CreateAsync(ClaimsPrincipal principal, long projectId, SaveTaskRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        if (!await OwnsProjectAsync(projectId, owner.Id, cancellationToken)) return new(TaskSaveStatus.NotFound);

        var now = UtcNow();
        if (request.DueDate < DateOnly.FromDateTime(now)) return new(TaskSaveStatus.PastDueDate);

        var task = new BoardTask { ProjectId = projectId, Title = request.Title!, CreatedAtUtc = now };
        Apply(task, request, now);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return new(TaskSaveStatus.Saved, ToDto(task));
    }

    public async Task<TaskSaveResult> UpdateAsync(ClaimsPrincipal principal, long taskId, UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        var task = await OwnedTasks(owner.Id).SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);
        if (task is null) return new(TaskSaveStatus.NotFound);

        var now = UtcNow();
        if (request.DueDate != task.DueDate && request.DueDate < DateOnly.FromDateTime(now))
        {
            return new(TaskSaveStatus.PastDueDate);
        }

        Apply(task, request, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(TaskSaveStatus.NotFound);
        }

        return new(TaskSaveStatus.Saved, ToDto(task));
    }

    public async Task<bool> DeleteAsync(ClaimsPrincipal principal, long taskId, CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        var task = await OwnedTasks(owner.Id).SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);
        if (task is null) return false;

        db.Tasks.Remove(task);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private Task<bool> OwnsProjectAsync(long projectId, long ownerId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == ownerId, cancellationToken);

    private IQueryable<BoardTask> OwnedTasks(long ownerId) => db.Tasks.Where(task => task.Project.OwnerId == ownerId);

    private DateTime UtcNow()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    private static void Apply(BoardTask task, SaveTaskRequest request, DateTime now)
    {
        task.Title = request.Title!.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        task.Status = request.Status is null ? BoardTaskStatus.ToDo : Enum.Parse<BoardTaskStatus>(request.Status);
        task.Priority = request.Priority is null ? TaskPriority.Medium : Enum.Parse<TaskPriority>(request.Priority);
        task.DueDate = request.DueDate;
        task.Assignee = string.IsNullOrWhiteSpace(request.Assignee) ? null : request.Assignee.Trim();
        task.UpdatedAtUtc = now;
    }

    private static TaskDto ToDto(BoardTask task) => new(task.Id, task.ProjectId, task.Title, task.Description,
        task.Status.ToString(), task.Priority.ToString(), task.DueDate, task.Assignee,
        DateTime.SpecifyKind(task.CreatedAtUtc, DateTimeKind.Utc), DateTime.SpecifyKind(task.UpdatedAtUtc, DateTimeKind.Utc));
}

public enum TaskSaveStatus { Saved, NotFound, PastDueDate }

public sealed record TaskSaveResult(TaskSaveStatus Status, TaskDto? Task = null);
