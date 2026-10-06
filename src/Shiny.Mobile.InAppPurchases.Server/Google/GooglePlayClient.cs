using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server
{
    /// <summary>
    /// Google Play Developer API (purchases). Requires <see cref="GooglePlayOptions.ServiceAccountJson"/>.
    /// </summary>
    public interface IGooglePlayClient
    {
        /// <summary>purchases.subscriptionsv2.get - null when the token is not found (404/410)</summary>
        Task<GoogleSubscriptionPurchase?> GetSubscriptionAsync(string purchaseToken, CancellationToken cancellationToken = default);

        /// <summary>purchases.productsv2.getproductpurchasev2 - null when the token is not found (404/410)</summary>
        Task<GoogleProductPurchase?> GetProductAsync(string purchaseToken, CancellationToken cancellationToken = default);

        /// <summary>purchases.subscriptions.acknowledge - the app normally acknowledges; use this as a server-side fallback</summary>
        Task AcknowledgeSubscriptionAsync(string subscriptionId, string purchaseToken, CancellationToken cancellationToken = default);

        /// <summary>purchases.products.acknowledge</summary>
        Task AcknowledgeProductAsync(string productId, string purchaseToken, CancellationToken cancellationToken = default);

        /// <summary>purchases.products.consume</summary>
        Task ConsumeProductAsync(string productId, string purchaseToken, CancellationToken cancellationToken = default);
    }


    public sealed class GooglePlayApiException(HttpStatusCode statusCode, string message) : Exception(message)
    {
        public HttpStatusCode StatusCode { get; } = statusCode;


        internal static async Task<GooglePlayApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string? message = null;
            try
            {
                var error = await response.Content.ReadFromJsonAsync(InAppPurchaseServerJsonContext.Default.GoogleApiErrorResponse, cancellationToken).ConfigureAwait(false);
                message = error?.Error?.Message;
            }
            catch (JsonException)
            {
            }
            return new(response.StatusCode, $"Google Play API returned {(int)response.StatusCode}: {message ?? response.ReasonPhrase}");
        }
    }
}


namespace Shiny.InAppPurchases.Server.Infrastructure
{
    sealed class GooglePlayClient(
        IHttpClientFactory httpClientFactory,
        IOptions<InAppPurchaseServerOptions> options,
        GoogleAccessTokenProvider tokens
    ) : IGooglePlayClient
    {
        public const string HttpClientName = "Shiny.InAppPurchases.Google";
        internal const string BaseUrl = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/";


        public Task<GoogleSubscriptionPurchase?> GetSubscriptionAsync(string purchaseToken, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(purchaseToken);
            return this.GetAsync(
                "purchases/subscriptionsv2/tokens/" + Uri.EscapeDataString(purchaseToken),
                InAppPurchaseServerJsonContext.Default.GoogleSubscriptionPurchase,
                cancellationToken
            );
        }


        public Task<GoogleProductPurchase?> GetProductAsync(string purchaseToken, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(purchaseToken);
            return this.GetAsync(
                "purchases/productsv2/tokens/" + Uri.EscapeDataString(purchaseToken),
                InAppPurchaseServerJsonContext.Default.GoogleProductPurchase,
                cancellationToken
            );
        }


        public Task AcknowledgeSubscriptionAsync(string subscriptionId, string purchaseToken, CancellationToken cancellationToken = default)
            => this.PostAsync($"purchases/subscriptions/{Uri.EscapeDataString(subscriptionId)}/tokens/{Uri.EscapeDataString(purchaseToken)}:acknowledge", cancellationToken);


        public Task AcknowledgeProductAsync(string productId, string purchaseToken, CancellationToken cancellationToken = default)
            => this.PostAsync($"purchases/products/{Uri.EscapeDataString(productId)}/tokens/{Uri.EscapeDataString(purchaseToken)}:acknowledge", cancellationToken);


        public Task ConsumeProductAsync(string productId, string purchaseToken, CancellationToken cancellationToken = default)
            => this.PostAsync($"purchases/products/{Uri.EscapeDataString(productId)}/tokens/{Uri.EscapeDataString(purchaseToken)}:consume", cancellationToken);


        async Task<T?> GetAsync<T>(string path, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken) where T : class
        {
            using var request = await this.CreateRequestAsync(HttpMethod.Get, path, cancellationToken).ConfigureAwait(false);
            using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                return null;

            if (!response.IsSuccessStatusCode)
                throw await GooglePlayApiException.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);

            return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);
        }


        async Task PostAsync(string path, CancellationToken cancellationToken)
        {
            using var request = await this.CreateRequestAsync(HttpMethod.Post, path, cancellationToken).ConfigureAwait(false);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw await GooglePlayApiException.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }


        async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
        {
            var google = options.Value.Google
                ?? throw new InvalidOperationException("Google Play is not configured - set InAppPurchaseServerOptions.Google");

            var token = await tokens.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var request = new HttpRequestMessage(method, BaseUrl + Uri.EscapeDataString(google.PackageName) + "/" + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }
    }


    static class GoogleServiceAccount
    {
        public static GoogleServiceAccountCredentials Parse(string json)
        {
            GoogleServiceAccountCredentials? credentials;
            try
            {
                credentials = JsonSerializer.Deserialize(json, InAppPurchaseServerJsonContext.Default.GoogleServiceAccountCredentials);
            }
            catch (JsonException ex)
            {
                throw new FormatException("Google.ServiceAccountJson is not valid JSON: " + ex.Message, ex);
            }

            if (credentials == null || String.IsNullOrWhiteSpace(credentials.ClientEmail) || String.IsNullOrWhiteSpace(credentials.PrivateKey))
                throw new FormatException("Google.ServiceAccountJson must be a service account key containing client_email and private_key");

            return credentials;
        }
    }


    /// <summary>
    /// OAuth2 service-account (JWT bearer grant) access tokens for the androidpublisher scope, cached until a minute before expiry.
    /// </summary>
    sealed class GoogleAccessTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<InAppPurchaseServerOptions> options,
        TimeProvider timeProvider
    ) : IDisposable
    {
        public const string Scope = "https://www.googleapis.com/auth/androidpublisher";
        const string DefaultTokenUri = "https://oauth2.googleapis.com/token";

        readonly SemaphoreSlim gate = new(1, 1);
        (GoogleServiceAccountCredentials Credentials, RSA Key)? account;
        (string Token, DateTimeOffset RefreshAt)? cached;


        public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
        {
            if (this.cached is { } c && timeProvider.GetUtcNow() < c.RefreshAt)
                return c.Token;

            await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var now = timeProvider.GetUtcNow();
                if (this.cached is { } c2 && now < c2.RefreshAt)
                    return c2.Token;

                var (credentials, key) = this.GetAccount();
                var tokenUri = String.IsNullOrWhiteSpace(credentials.TokenUri) ? DefaultTokenUri : credentials.TokenUri;

                var assertion = JwtHelper.CreateRs256(key, credentials.PrivateKeyId, w =>
                {
                    w.WriteString("iss", credentials.ClientEmail);
                    w.WriteString("scope", Scope);
                    w.WriteString("aud", tokenUri);
                    w.WriteNumber("iat", now.ToUnixTimeSeconds());
                    w.WriteNumber("exp", now.AddHours(1).ToUnixTimeSeconds());
                });

                using var content = new FormUrlEncodedContent([
                    new("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                    new("assertion", assertion)
                ]);
                using var response = await httpClientFactory
                    .CreateClient(GooglePlayClient.HttpClientName)
                    .PostAsync(tokenUri, content, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new GooglePlayApiException(response.StatusCode, $"Google OAuth token exchange failed with {(int)response.StatusCode} - check Google.ServiceAccountJson");

                var result = await response.Content.ReadFromJsonAsync(InAppPurchaseServerJsonContext.Default.GoogleTokenResponse, cancellationToken).ConfigureAwait(false);
                if (String.IsNullOrWhiteSpace(result?.AccessToken))
                    throw new GooglePlayApiException(response.StatusCode, "Google OAuth token response did not contain an access_token");

                var lifetime = TimeSpan.FromSeconds(result.ExpiresIn > 0 ? result.ExpiresIn : 3600);
                this.cached = (result.AccessToken, now + lifetime - TimeSpan.FromSeconds(60));
                return result.AccessToken;
            }
            finally
            {
                this.gate.Release();
            }
        }


        (GoogleServiceAccountCredentials, RSA) GetAccount()
        {
            if (this.account is { } a)
                return a;

            var google = options.Value.Google
                ?? throw new InvalidOperationException("Google Play is not configured - set InAppPurchaseServerOptions.Google");

            if (!google.HasServiceAccount)
                throw new InvalidOperationException("Google.ServiceAccountJson is required to call the Google Play Developer API");

            var credentials = GoogleServiceAccount.Parse(google.ServiceAccountJson!);
            var key = RSA.Create();
            key.ImportFromPem(credentials.PrivateKey!.Replace("\\n", "\n"));
            this.account = (credentials, key);
            return (credentials, key);
        }


        public void Dispose()
        {
            this.account?.Key.Dispose();
            this.gate.Dispose();
        }
    }
}
