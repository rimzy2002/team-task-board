namespace TeamTaskBoard.Api.Data;

public sealed class Project
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
