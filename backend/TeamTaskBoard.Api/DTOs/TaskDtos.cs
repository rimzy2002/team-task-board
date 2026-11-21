using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;
using TeamTaskBoard.Api.Data;

namespace TeamTaskBoard.Api.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class SaveTaskRequest : IValidatableObject
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? Assignee { get; init; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title) || Title.Trim().Length > 200)
        {
            yield return new ValidationResult("Enter a title of 1 to 200 characters.", [nameof(Title)]);
        }

        if (Description is not null && Encoding.UTF8.GetByteCount(Description.Trim()) > 65_535)
        {
            yield return new ValidationResult("Use at most 65,535 UTF-8 bytes for the description.", [nameof(Description)]);
        }

        if (Assignee?.Trim().Length > 200)
        {
            yield return new ValidationResult("Use 200 characters or fewer for the assignee.", [nameof(Assignee)]);
        }

        foreach (var error in TaskInput.ValidateStatusAndPriority(Status, Priority))
        {
            yield return error;
        }
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateTaskRequest : SaveTaskRequest
{
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in base.Validate(validationContext))
        {
            yield return error;
        }

        if (Status is null) yield return new ValidationResult("Status is required for an update.", [nameof(Status)]);
        if (Priority is null) yield return new ValidationResult("Priority is required for an update.", [nameof(Priority)]);
    }
}

public sealed class TaskListQuery : IValidatableObject
{
    public const int PageSize = 10;

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Status { get; init; }

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Priority { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in TaskInput.ValidateStatusAndPriority(Status, Priority))
        {
            yield return error;
        }

        if (Search?.Trim().Length > 200)
        {
            yield return new ValidationResult("Use 200 characters or fewer for the search.", [nameof(Search)]);
        }

        if (Page < 1 || Page > int.MaxValue / PageSize + 1)
        {
            yield return new ValidationResult("The page number is outside the supported range.", [nameof(Page)]);
        }
    }
}

public sealed record TaskDto(long Id, long ProjectId, string Title, string? Description, string Status, string Priority,
    DateOnly? DueDate, string? Assignee, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record TaskPageDto(IReadOnlyList<TaskDto> Items, int Page, int PageSize, int TotalCount);

internal static class TaskInput
{
    internal static IEnumerable<ValidationResult> ValidateStatusAndPriority(string? status, string? priority)
    {
        // Match the canonical enum names. Enum.TryParse also accepts numbers and comma-separated names.
        if (status is not null && !Enum.GetNames<BoardTaskStatus>().Contains(status))
        {
            yield return new ValidationResult("Status must be ToDo, InProgress, or Done.", [nameof(SaveTaskRequest.Status)]);
        }

        if (priority is not null && !Enum.GetNames<TaskPriority>().Contains(priority))
        {
            yield return new ValidationResult("Priority must be Low, Medium, or High.", [nameof(SaveTaskRequest.Priority)]);
        }
    }
}
