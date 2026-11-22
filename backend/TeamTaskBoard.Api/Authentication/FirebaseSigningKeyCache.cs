using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace TeamTaskBoard.Api.Authentication;

internal sealed class FirebaseSigningKeyCache(
    IHttpClientFactory httpClientFactory, TimeProvider clock, ILogger<FirebaseSigningKeyCache> logger) : IDisposable
{
    private const string CertificateUrl = "https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private volatile KeySnapshot? snapshot;
    private DateTimeOffset nextRefresh;

    public async Task<RsaSecurityKey?> GetKeyAsync(string kid)
    {
        var cached = FindKey(kid, clock.GetUtcNow());
        if (cached is not null)
        {
            return cached;
        }

        await refreshLock.WaitAsync();
        try
        {
            var now = clock.GetUtcNow();
            cached = FindKey(kid, now);
            if (cached is not null || now < nextRefresh)
            {
                return cached;
            }

            nextRefresh = now + RefreshInterval;
            await RefreshAsync(now);
            return FindKey(kid, clock.GetUtcNow());
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private RsaSecurityKey? FindKey(string kid, DateTimeOffset now)
    {
        var current = snapshot;
        return current is not null && current.ExpiresAt > now && current.Keys.TryGetValue(kid, out var key)
            ? key
            : null;
    }

    private async Task RefreshAsync(DateTimeOffset requestedAt)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(FirebaseAuthentication.SigningKeysClient);
            using var response = await client.GetAsync(CertificateUrl);
            response.EnsureSuccessStatusCode();

            var cacheControl = response.Headers.CacheControl;
            var ttl = cacheControl?.MaxAge - (response.Headers.Age ?? TimeSpan.Zero);
            if (cacheControl is null || cacheControl.NoCache || cacheControl.NoStore || ttl is null || ttl <= TimeSpan.Zero)
            {
                logger.LogWarning("Firebase signing certificate response did not have a usable cache lifetime.");
                return;
            }

            var certificates = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            var keys = new Dictionary<string, RsaSecurityKey>(StringComparer.Ordinal);
            foreach (var (kid, pem) in certificates ?? [])
            {
                using var certificate = X509Certificate2.CreateFromPem(pem);
                using var rsa = certificate.GetRSAPublicKey();
                if (rsa is not null && rsa.KeySize >= 2048)
                {
                    keys.Add(kid, new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = kid });
                }
            }

            if (keys.Count > 0)
            {
                snapshot = new KeySnapshot(keys, requestedAt + ttl.Value);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
            or JsonException or CryptographicException or ArgumentException or InvalidOperationException or IOException)
        {
            logger.LogWarning("Firebase signing certificates could not be refreshed.");
        }
    }

    public void Dispose() => refreshLock.Dispose();

    private sealed record KeySnapshot(Dictionary<string, RsaSecurityKey> Keys, DateTimeOffset ExpiresAt);
}
