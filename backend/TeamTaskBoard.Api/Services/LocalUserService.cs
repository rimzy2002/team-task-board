using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using TeamTaskBoard.Api.Data;

namespace TeamTaskBoard.Api.Services;

public sealed class LocalUserService(TeamTaskBoardContext db, TimeProvider clock)
{
    public async Task<User> SynchronizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var uid = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The authenticated principal has no Firebase UID.");
        var verifiedEmail = GetVerifiedEmail(principal);
        var users = db.Users.Where(user => user.FirebaseUid == uid);
        var localUser = await users.AsNoTracking().SingleOrDefaultAsync(cancellationToken);

        if (localUser is null)
        {
            var createdAt = clock.GetUtcNow().UtcDateTime;
            // A concurrent insert can win the unique UID check. Leave its row intact and read it below.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO users (firebase_uid, email, created_at_utc)
                VALUES ({uid}, {verifiedEmail}, {createdAt})
                ON DUPLICATE KEY UPDATE id = id
                """, cancellationToken);

            localUser = await users.AsNoTracking().SingleAsync(cancellationToken);
        }

        if (verifiedEmail is not null && !string.Equals(verifiedEmail, localUser.Email, StringComparison.Ordinal))
        {
            await users.ExecuteUpdateAsync(setters => setters.SetProperty(user => user.Email, verifiedEmail), cancellationToken);
            localUser.Email = verifiedEmail;
        }

        return localUser;
    }

    private static string? GetVerifiedEmail(ClaimsPrincipal principal)
    {
        var verified = principal.FindFirst("email_verified");
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
        return verified?.ValueType == ClaimValueTypes.Boolean && bool.TryParse(verified.Value, out var isVerified)
            && isVerified && !string.IsNullOrWhiteSpace(email) && email.Length <= 254 && !email.Any(char.IsControl)
            ? email
            : null;
    }
}
