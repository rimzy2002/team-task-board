using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TeamTaskBoard.Api.Authentication;

internal static class FirebaseAuthentication
{
    internal const string SigningKeysClient = "FirebaseSigningKeys";

    public static void AddFirebaseAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"]?.Trim();

        services.AddHttpClient(SigningKeysClient, client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FirebaseSigningKeyCache>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorization();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<FirebaseSigningKeyCache, TimeProvider>((options, signingKeys, clock) =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.IncludeErrorDetails = false;
                options.RefreshOnIssuerKeyNotFound = false;
                options.TokenHandlers.Clear();
                options.TokenHandlers.Add(new FirebaseTokenHandler(projectId, signingKeys, clock)
                {
                    MapInboundClaims = false,
                });
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = $"https://securetoken.google.com/{projectId}",
                    ValidateIssuer = true,
                    ValidAudience = projectId,
                    ValidateAudience = true,
                    IgnoreTrailingSlashWhenValidatingAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    TryAllIssuerSigningKeys = false,
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized)
                            .ExecuteAsync(context.HttpContext);
                    },
                };
            });
    }
}
