using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TeamTaskBoard.Api.Authentication;

internal sealed class FirebaseTokenHandler(string? projectId, FirebaseSigningKeyCache signingKeys, TimeProvider clock)
    : JsonWebTokenHandler
{
    public override async Task<TokenValidationResult> ValidateTokenAsync(
        string token, TokenValidationParameters validationParameters)
    {
        if (string.IsNullOrEmpty(projectId) || !CanReadToken(token))
        {
            return InvalidToken();
        }

        JsonWebToken jwt;
        try
        {
            jwt = ReadJsonWebToken(token);
        }
        catch (ArgumentException)
        {
            return InvalidToken();
        }
        catch (SecurityTokenException)
        {
            return InvalidToken();
        }

        if (jwt.Alg != SecurityAlgorithms.RsaSha256 || string.IsNullOrWhiteSpace(jwt.Kid))
        {
            return InvalidToken();
        }

        var key = await signingKeys.GetKeyAsync(jwt.Kid);
        if (key is null)
        {
            return InvalidToken();
        }

        var parameters = validationParameters.Clone();
        parameters.IssuerSigningKey = key;
        var result = await base.ValidateTokenAsync(jwt, parameters);

        return result.IsValid && !HasFirebaseClaims(jwt) ? InvalidToken() : result;
    }

    private bool HasFirebaseClaims(JsonWebToken jwt)
    {
        // Inspect the verified JSON directly so numeric strings and arrays cannot be coerced into claims.
        using var payload = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(jwt.EncodedPayload));
        var claims = payload.RootElement;
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        return TryGetString(claims, JwtRegisteredClaimNames.Aud, out var audience)
            && string.Equals(audience, projectId, StringComparison.Ordinal)
            && TryGetString(claims, JwtRegisteredClaimNames.Iss, out var issuer)
            && string.Equals(issuer, $"https://securetoken.google.com/{projectId}", StringComparison.Ordinal)
            && TryGetString(claims, JwtRegisteredClaimNames.Sub, out var uid)
            && !string.IsNullOrWhiteSpace(uid) && uid.Length <= 128 && !uid.Any(char.IsControl)
            && TryGetTime(claims, JwtRegisteredClaimNames.Exp, out var expiresAt) && expiresAt > now
            && TryGetTime(claims, JwtRegisteredClaimNames.Iat, out var issuedAt) && issuedAt <= now
            && TryGetTime(claims, "auth_time", out var authenticatedAt) && authenticatedAt <= issuedAt;
    }

    private static bool TryGetString(JsonElement claims, string name, [NotNullWhen(true)] out string? value)
    {
        value = claims.TryGetProperty(name, out var claim) && claim.ValueKind == JsonValueKind.String
            ? claim.GetString()
            : null;
        return value is not null;
    }

    private static bool TryGetTime(JsonElement claims, string name, out long seconds)
    {
        seconds = 0;
        return claims.TryGetProperty(name, out var claim)
            && claim.ValueKind == JsonValueKind.Number && claim.TryGetInt64(out seconds) && seconds > 0;
    }

    private static TokenValidationResult InvalidToken() => new()
    {
        Exception = new SecurityTokenValidationException("Invalid Firebase ID token."),
    };
}
