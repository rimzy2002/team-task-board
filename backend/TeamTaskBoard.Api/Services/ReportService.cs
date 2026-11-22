using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.DTOs;

namespace TeamTaskBoard.Api.Services;

public sealed class ReportService(TeamTaskBoardContext db, LocalUserService localUsers, TimeProvider clock)
{
    public async Task<List<OverdueAssigneeDto>> ListOverdueByUserAsync(
        ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var owner = await localUsers.SynchronizeAsync(principal, cancellationToken);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var midnightUtc = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var done = nameof(BoardTaskStatus.Done);

        return await db.Database.SqlQuery<OverdueAssigneeDto>($"""
            SELECT NULLIF(REGEXP_REPLACE(t.assignee, '^[[:space:]]+|[[:space:]]+$', ''), '')
                       COLLATE utf8mb4_0900_bin AS AssigneeName,
                   COUNT(*) AS OverdueTaskCount
            FROM projects AS p
            INNER JOIN tasks AS t ON t.project_id = p.id
            WHERE p.owner_id = {owner.Id}
              AND t.due_date IS NOT NULL
              AND t.due_date < CAST({midnightUtc} AS DATE)
              AND t.status <> {done}
            GROUP BY AssigneeName
            ORDER BY OverdueTaskCount DESC, AssigneeName IS NULL, AssigneeName
            """).ToListAsync(cancellationToken);
    }
}
