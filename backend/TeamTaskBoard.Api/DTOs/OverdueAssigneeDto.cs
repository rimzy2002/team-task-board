namespace TeamTaskBoard.Api.DTOs;

public sealed record OverdueAssigneeDto(string? AssigneeName, long OverdueTaskCount);
