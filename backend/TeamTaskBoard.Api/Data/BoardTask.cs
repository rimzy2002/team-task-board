namespace TeamTaskBoard.Api.Data;

public sealed class BoardTask
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public BoardTaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Assignee { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public enum BoardTaskStatus
{
    ToDo,
    InProgress,
    Done,
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
}
