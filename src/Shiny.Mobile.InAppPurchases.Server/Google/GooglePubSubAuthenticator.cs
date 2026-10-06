using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Shiny.InAppPurchases.Server.Infrastructure;


/// <summary>
/// Validates the Google-signed OIDC token Pub/Sub attaches to authenticated push requests.
/// </summary>
sealed class GooglePubSubAuthenticator(
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    ILogger<GooglePubSubAuthenticator> logger
)
{
    public const string JwksUrl = "https://www.googleapis.com/oauth2/v3/certs";
    static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);
    static readonly TimeSpan DefaultKeyLifetime = TimeSpan.FromHours(1);
    static readonly TimeSpan MinUnknownKidRefresh = TimeSpan.FromSeconds(30);

    readonly SemaphoreSlim gate = new(1, 1);
    KeySet? keys;

    sealed record KeySet(IReadOnlyDictionary<string, RSAParameters> Keys, DateTimeOffset ExpiresAt, DateTimeOffset FetchedAt);


    /// <returns>null when the token is valid, otherwise the rejection reason</returns>
    public async Task<string?> ValidateAsync(string? authorization, GooglePlayOptions google, CancellationToken cancellationToken)
    {
        const string prefix = "Bearer ";
        if (String.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return "Missing bearer token";

        var parts = authorization[prefix.Length..].Trim().Split('.');
        if (parts.Length != 3)
            return "Malformed token";

        string? alg, kid;
        byte[] signature;
        JsonDocument claims;
        try
        {
            using (var header = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[0])))
            {
                alg = GetString(header.RootElement, "alg");
                kid = GetString(header.RootElement, "kid");
            }
            signature = Base64Url.DecodeFromChars(parts[2]);
            claims = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return "Malformed token";
        }

        using (claims)
        {
            if (alg != "RS256")
                return $"Unsupported algorithm '{alg}'";

            if (String.IsNullOrWhiteSpace(kid))
                return "Token has no key id";

            var key = await this.GetKeyAsync(kid, cancellationToken).ConfigureAwait(false);
            if (key == null)
                return "Unknown signing key";

            using (var rsa = RSA.Create(key.Value))
            {
                var input = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                if (!rsa.VerifyData(input, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    return "Invalid signature";
            }
            return ValidateClaims(claims.RootElement, google, timeProvider.GetUtcNow());
        }
    }


    static string? ValidateClaims(JsonElement claims, GooglePlayOptions google, DateTimeOffset now)
    {
        if (claims.ValueKind != JsonValueKind.Object)
            return "Malformed claims";

        var iss = GetString(claims, "iss");
        if (iss is not ("accounts.google.com" or "https://accounts.google.com"))
            return "Invalid issuer";

        if (!AudienceMatches(claims, google.PubSubAudience))
            return "Invalid audience";

        if (GetUnixTime(claims, "exp") is not { } exp)
            return "Token has no expiry";

        if (now > exp + ClockSkew)
            return "Token expired";

        if (GetUnixTime(claims, "nbf") is { } nbf && now + ClockSkew < nbf)
            return "Token not yet valid";

        if (GetUnixTime(claims, "iat") is { } iat && now + ClockSkew < iat)
            return "Token issued in the future";

        if (!String.Equals(GetString(claims, "email"), google.PubSubServiceAccountEmail, StringComparison.OrdinalIgnoreCase))
            return "Invalid service account email";

        var verified = claims.TryGetProperty("email_verified", out var ev) &&
            (ev.ValueKind == JsonValueKind.True || (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true"));

        return verified ? null : "Service account email is not verified";
    }


    async Task<RSAParameters?> GetKeyAsync(string kid, CancellationToken cancellationToken)
    {
        var current = this.keys;
        if (current != null && timeProvider.GetUtcNow() < current.ExpiresAt && current.Keys.TryGetValue(kid, out var cachedKey))
            return cachedKey;

        await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = timeProvider.GetUtcNow();
            current = this.keys;
            if (current != null && now < current.ExpiresAt)
            {
                if (current.Keys.TryGetValue(kid, out var key))
                    return key;

                // unknown kid while the cache is fresh - Google may have rotated, but don't let bad tokens hammer the endpoint
                if (now - current.FetchedAt < MinUnknownKidRefresh)
                    return null;
            }

            this.keys = current = await this.FetchAsync(now, cancellationToken).ConfigureAwait(false);
            return current.Keys.TryGetValue(kid, out var fresh) ? fresh : null;
        }
        finally
        {
            this.gate.Release();
        }
    }


    async Task<KeySet> FetchAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory
            .CreateClient(GooglePlayClient.HttpClientName)
            .GetAsync(JwksUrl, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var lifetime = response.Headers.CacheControl?.MaxAge ?? DefaultKeyLifetime;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var result = new Dictionary<string, RSAParameters>(StringComparer.Ordinal);
        if (doc.RootElement.TryGetProperty("keys", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var jwk in list.EnumerateArray())
            {
                var kid = GetString(jwk, "kid");
                var n = GetString(jwk, "n");
                var e = GetString(jwk, "e");
                if (GetString(jwk, "kty") != "RSA" || kid == null || n == null || e == null)
                    continue;

                try
                {
                    result[kid] = new RSAParameters
                    {
                        Modulus = Base64Url.DecodeFromChars(n),
                        Exponent = Base64Url.DecodeFromChars(e)
                    };
                }
                catch (FormatException)
                {
                    logger.LogWarning("Skipping malformed Google JWKS key {Kid}", kid);
                }
            }
        }
        logger.LogDebug("Fetched {Count} Google OIDC signing keys, cached for {Lifetime}", result.Count, lifetime);
        return new KeySet(result, now + lifetime, now);
    }


    static bool AudienceMatches(JsonElement claims, string? expected)
    {
        if (String.IsNullOrEmpty(expected) || !claims.TryGetProperty("aud", out var aud))
            return false;

        if (aud.ValueKind == JsonValueKind.String)
            return aud.GetString() == expected;

        if (aud.ValueKind == JsonValueKind.Array)
            return aud.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString() == expected);

        return false;
    }


    static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;


    static DateTimeOffset? GetUnixTime(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}
