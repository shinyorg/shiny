using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server
{
    /// <summary>
    /// App Store verification (offline JWS) and the App Store Server API.
    /// </summary>
    public interface IAppleStoreClient
    {
        /// <summary>Verifies an App Store Server Notification V2 signedPayload, its bundle id, environment and (Production) appAppleId</summary>
        /// <exception cref="AppleVerificationException">The payload is not a valid Apple-signed notification for this app</exception>
        AppleNotificationPayload VerifyNotification(string signedPayload);

        /// <summary>Verifies a signed transaction (StoreKit 2 <c>jwsRepresentation</c> / signedTransactionInfo)</summary>
        /// <exception cref="AppleVerificationException">The transaction is not a valid Apple-signed transaction for this app</exception>
        AppleTransaction VerifyTransaction(string signedTransaction);

        /// <summary>Verifies signedRenewalInfo</summary>
        /// <exception cref="AppleVerificationException">The renewal info is not valid Apple-signed data</exception>
        AppleRenewalInfo VerifyRenewalInfo(string signedRenewalInfo);

        /// <summary>Get Transaction Info - null when the transaction does not exist. Requires Server API credentials.</summary>
        Task<AppleTransaction?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>Get All Subscription Statuses for any transaction id in the subscription - null when not found. Requires Server API credentials.</summary>
        Task<IReadOnlyList<AppleSubscriptionStatus>?> GetSubscriptionStatusesAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>Asks Apple to send a TEST notification to the URL configured in App Store Connect. Returns the test notification token.</summary>
        Task<string> RequestTestNotificationAsync(bool sandbox = false, CancellationToken cancellationToken = default);
    }


    public sealed class AppleStoreApiException(HttpStatusCode statusCode, long? errorCode, string message) : Exception(message)
    {
        public HttpStatusCode StatusCode { get; } = statusCode;

        /// <summary>Apple's numeric errorCode (e.g. 4040010 TransactionIdNotFound)</summary>
        public long? ErrorCode { get; } = errorCode;
    }
}


namespace Shiny.InAppPurchases.Server.Infrastructure
{
    sealed class AppStoreClient : IAppleStoreClient
    {
        public const string HttpClientName = "Shiny.InAppPurchases.Apple";
        internal const string ProductionUrl = "https://api.storekit.itunes.apple.com";
        internal const string SandboxUrl = "https://api.storekit-sandbox.itunes.apple.com";

        readonly IHttpClientFactory httpClientFactory;
        readonly IOptions<InAppPurchaseServerOptions> options;
        readonly ILogger logger;
        readonly Lazy<AppleJwsVerifier> verifier;
        readonly Lazy<AppleServerApiTokenProvider> tokens;


        public AppStoreClient(
            IHttpClientFactory httpClientFactory,
            IOptions<InAppPurchaseServerOptions> options,
            TimeProvider timeProvider,
            ILogger<AppStoreClient> logger
        )
        {
            this.httpClientFactory = httpClientFactory;
            this.options = options;
            this.logger = logger;
            this.verifier = new(() => new AppleJwsVerifier(
                this.Apple.TrustedRootsOverride ?? [AppleJwsVerifier.LoadAppleRootCertificate()],
                this.Apple.EnableOnlineRevocationCheck,
                timeProvider
            ));
            this.tokens = new(() => new AppleServerApiTokenProvider(this.Apple, timeProvider));
        }


        AppleStoreOptions Apple => this.options.Value.Apple
            ?? throw new InvalidOperationException("The App Store is not configured - set InAppPurchaseServerOptions.Apple");


        public AppleNotificationPayload VerifyNotification(string signedPayload)
        {
            var apple = this.Apple;
            var payload = this.verifier.Value.Verify(signedPayload, InAppPurchaseServerJsonContext.Default.AppleNotificationPayload, x => x.SignedDate);

            string? bundleId;
            long? appAppleId;
            StoreEnvironment environment;

            if (payload.Data != null)
            {
                bundleId = payload.Data.BundleId;
                appAppleId = payload.Data.AppAppleId;
                environment = InAppPurchaseUtils.ParseAppleEnvironment(payload.Data.Environment);
            }
            else if (payload.Summary != null)
            {
                bundleId = payload.Summary.BundleId;
                appAppleId = payload.Summary.AppAppleId;
                environment = InAppPurchaseUtils.ParseAppleEnvironment(payload.Summary.Environment);
            }
            else if (payload.ExternalPurchaseToken != null)
            {
                bundleId = payload.ExternalPurchaseToken.BundleId;
                appAppleId = payload.ExternalPurchaseToken.AppAppleId;
                environment = payload.ExternalPurchaseToken.ExternalPurchaseId?.StartsWith("SANDBOX", StringComparison.Ordinal) == true
                    ? StoreEnvironment.Sandbox
                    : StoreEnvironment.Production;
            }
            else
            {
                throw AppleJwsVerifier.Fail(AppleVerificationFailure.InvalidFormat, "Notification has no data, summary or externalPurchaseToken");
            }

            CheckBundleId(apple, bundleId);
            CheckEnvironment(apple, environment);

            if (environment == StoreEnvironment.Production)
            {
                if (apple.AppAppleId == null)
                    throw AppleJwsVerifier.Fail(AppleVerificationFailure.InvalidAppAppleId, "Apple.AppAppleId must be configured to accept Production notifications");

                if (appAppleId != apple.AppAppleId)
                    throw AppleJwsVerifier.Fail(AppleVerificationFailure.InvalidAppAppleId, "Notification appAppleId does not match Apple.AppAppleId");
            }
            return payload;
        }


        public AppleTransaction VerifyTransaction(string signedTransaction)
        {
            var apple = this.Apple;
            var transaction = this.verifier.Value.Verify(signedTransaction, InAppPurchaseServerJsonContext.Default.AppleTransaction, x => x.SignedDate);
            CheckBundleId(apple, transaction.BundleId);
            CheckEnvironment(apple, InAppPurchaseUtils.ParseAppleEnvironment(transaction.Environment));
            return transaction;
        }


        public AppleRenewalInfo VerifyRenewalInfo(string signedRenewalInfo)
        {
            var apple = this.Apple;
            var renewal = this.verifier.Value.Verify(signedRenewalInfo, InAppPurchaseServerJsonContext.Default.AppleRenewalInfo, x => x.SignedDate);
            CheckEnvironment(apple, InAppPurchaseUtils.ParseAppleEnvironment(renewal.Environment));
            return renewal;
        }


        public async Task<AppleTransaction?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
            var response = await this.SendAsync(
                HttpMethod.Get,
                "/inApps/v1/transactions/" + Uri.EscapeDataString(transactionId),
                InAppPurchaseServerJsonContext.Default.AppleTransactionInfoResponse,
                null,
                cancellationToken
            ).ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(response?.SignedTransactionInfo))
                return null;

            return this.VerifyTransaction(response.SignedTransactionInfo);
        }


        public async Task<IReadOnlyList<AppleSubscriptionStatus>?> GetSubscriptionStatusesAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
            var response = await this.SendAsync(
                HttpMethod.Get,
                "/inApps/v1/subscriptions/" + Uri.EscapeDataString(transactionId),
                InAppPurchaseServerJsonContext.Default.AppleStatusResponse,
                null,
                cancellationToken
            ).ConfigureAwait(false);

            if (response == null)
                return null;

            var list = new List<AppleSubscriptionStatus>();
            foreach (var group in response.Data ?? [])
            {
                foreach (var last in group.LastTransactions ?? [])
                {
                    if (String.IsNullOrWhiteSpace(last.SignedTransactionInfo) || String.IsNullOrWhiteSpace(last.SignedRenewalInfo))
                        continue;

                    list.Add(new AppleSubscriptionStatus(
                        group.SubscriptionGroupIdentifier,
                        last.OriginalTransactionId,
                        Enum.IsDefined((AppleSubscriptionState)last.Status) ? (AppleSubscriptionState)last.Status : AppleSubscriptionState.Unknown,
                        this.VerifyTransaction(last.SignedTransactionInfo),
                        this.VerifyRenewalInfo(last.SignedRenewalInfo)
                    ));
                }
            }
            return list;
        }


        public async Task<string> RequestTestNotificationAsync(bool sandbox = false, CancellationToken cancellationToken = default)
        {
            var response = await this.SendAsync(
                HttpMethod.Post,
                "/inApps/v1/notifications/test",
                InAppPurchaseServerJsonContext.Default.AppleTestNotificationResponse,
                sandbox,
                cancellationToken
            ).ConfigureAwait(false);

            return response?.TestNotificationToken
                ?? throw new AppleStoreApiException(HttpStatusCode.NotFound, null, "App Store Server API did not return a test notification token");
        }


        async Task<T?> SendAsync<T>(HttpMethod method, string path, JsonTypeInfo<T> typeInfo, bool? sandbox, CancellationToken cancellationToken) where T : class
        {
            var apple = this.Apple;
            if (!apple.HasServerApiCredentials)
                throw new InvalidOperationException("App Store Server API credentials are not configured - set Apple.IssuerId, Apple.KeyId and Apple.PrivateKey");

            string[] baseUrls = sandbox switch
            {
                true => [SandboxUrl],
                false => [ProductionUrl],
                null when apple.UseSandboxServerApi => [SandboxUrl],
                null => [ProductionUrl, SandboxUrl]
            };

            var http = this.httpClientFactory.CreateClient(HttpClientName);
            for (var i = 0; i < baseUrls.Length; i++)
            {
                using var request = new HttpRequestMessage(method, baseUrls[i] + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.tokens.Value.GetToken());

                using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    if (i < baseUrls.Length - 1)
                    {
                        this.logger.LogDebug("App Store Server API {Path} not found in production - retrying sandbox", method.Method);
                        continue;
                    }
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    AppleApiError? error = null;
                    try
                    {
                        error = await response.Content.ReadFromJsonAsync(InAppPurchaseServerJsonContext.Default.AppleApiError, cancellationToken).ConfigureAwait(false);
                    }
                    catch (JsonException)
                    {
                    }
                    throw new AppleStoreApiException(
                        response.StatusCode,
                        error?.ErrorCode,
                        $"App Store Server API returned {(int)response.StatusCode}: {error?.ErrorMessage ?? response.ReasonPhrase}"
                    );
                }

                return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);
            }
            return null;
        }


        static void CheckBundleId(AppleStoreOptions apple, string? bundleId)
        {
            if (!String.Equals(bundleId, apple.BundleId, StringComparison.Ordinal))
                throw AppleJwsVerifier.Fail(AppleVerificationFailure.InvalidBundleId, $"Bundle id '{bundleId}' does not match Apple.BundleId");
        }


        static void CheckEnvironment(AppleStoreOptions apple, StoreEnvironment environment)
        {
            var ok = environment switch
            {
                StoreEnvironment.Production => true,
                StoreEnvironment.Sandbox => apple.AllowSandbox,
                _ => false
            };
            if (!ok)
                throw AppleJwsVerifier.Fail(AppleVerificationFailure.InvalidEnvironment, $"Environment '{environment}' is not accepted");
        }
    }


    /// <summary>
    /// ES256 bearer tokens for the App Store Server API, cached until shortly before expiry.
    /// </summary>
    sealed class AppleServerApiTokenProvider(AppleStoreOptions apple, TimeProvider timeProvider)
    {
        public const string Audience = "appstoreconnect-v1";
        static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(20);

        readonly ECDsa key = LoadKey(apple.PrivateKey ?? String.Empty);
        readonly Lock sync = new();
        (string Token, DateTimeOffset RefreshAt)? cached;


        public string GetToken()
        {
            lock (this.sync)
            {
                var now = timeProvider.GetUtcNow();
                if (this.cached is { } c && now < c.RefreshAt)
                    return c.Token;

                var token = JwtHelper.CreateEs256(this.key, apple.KeyId!, w =>
                {
                    w.WriteString("iss", apple.IssuerId);
                    w.WriteNumber("iat", now.ToUnixTimeSeconds());
                    w.WriteNumber("exp", now.Add(Lifetime).ToUnixTimeSeconds());
                    w.WriteString("aud", Audience);
                    w.WriteString("bid", apple.BundleId);
                });
                this.cached = (token, now.Add(Lifetime).AddMinutes(-1));
                return token;
            }
        }


        public static ECDsa LoadKey(string pem)
        {
            var key = ECDsa.Create();
            try
            {
                // keys pasted into environment variables/JSON often carry escaped newlines
                key.ImportFromPem(pem.Replace("\\n", "\n"));
                return key;
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }
    }
}
