using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server;


public sealed class InAppPurchaseServerOptions
{
    /// <summary>App Store configuration. Leave null to disable the App Store endpoint.</summary>
    public AppleStoreOptions? Apple { get; set; }

    /// <summary>Google Play configuration. Leave null to disable the Google Play endpoint.</summary>
    public GooglePlayOptions? Google { get; set; }

    /// <summary>How long a processed notification id is remembered by the in-memory de-duplicator</summary>
    public TimeSpan DeduplicationWindow { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Maximum notification ids remembered by the in-memory de-duplicator</summary>
    public int DeduplicationCapacity { get; set; } = 100_000;
}


public sealed class AppleStoreOptions
{
    /// <summary>Your app's bundle identifier - every notification and transaction must carry it</summary>
    public string BundleId { get; set; } = String.Empty;

    /// <summary>
    /// The numeric Apple ID of your app (App Store Connect → App Information → Apple ID).
    /// Required to accept Production notifications.
    /// </summary>
    public long? AppAppleId { get; set; }

    /// <summary>Accept Sandbox (TestFlight / sandbox tester) notifications and transactions. Default true.</summary>
    public bool AllowSandbox { get; set; } = true;

    /// <summary>
    /// Only call the sandbox App Store Server API. By default lookups go to production first and fall back to
    /// sandbox when the transaction is not found.
    /// </summary>
    public bool UseSandboxServerApi { get; set; }

    /// <summary>Check certificate revocation online (OCSP/CRL) and validate against the current time instead of the signed date</summary>
    public bool EnableOnlineRevocationCheck { get; set; }

    /// <summary>App Store Connect → Users and Access → Integrations → In-App Purchase → Issuer ID</summary>
    public string? IssuerId { get; set; }

    /// <summary>The In-App Purchase key id</summary>
    public string? KeyId { get; set; }

    /// <summary>Contents of the downloaded SubscriptionKey_XXXX.p8 file (PEM)</summary>
    public string? PrivateKey { get; set; }

    public bool HasServerApiCredentials =>
        !String.IsNullOrWhiteSpace(this.IssuerId) &&
        !String.IsNullOrWhiteSpace(this.KeyId) &&
        !String.IsNullOrWhiteSpace(this.PrivateKey);

    // tests only - production always trusts the embedded Apple Root CA - G3
    internal IReadOnlyList<X509Certificate2>? TrustedRootsOverride { get; set; }
}


public sealed class GooglePlayOptions
{
    /// <summary>Your app's package name - every RTDN must carry it</summary>
    public string PackageName { get; set; } = String.Empty;

    /// <summary>
    /// Service account key JSON (Google Cloud → IAM → Service Accounts → Keys) for a service account invited in
    /// Play Console with "View financial data" + "Manage orders and subscriptions". Required to fetch purchase details
    /// and to verify purchases.
    /// </summary>
    public string? ServiceAccountJson { get; set; }

    /// <summary>Validate the OIDC token Pub/Sub attaches to push requests. Default true - only disable for local testing.</summary>
    public bool RequirePubSubAuthentication { get; set; } = true;

    /// <summary>The audience configured on the Pub/Sub push subscription (defaults to the push endpoint URL in Google Cloud)</summary>
    public string? PubSubAudience { get; set; }

    /// <summary>The service account email configured on the Pub/Sub push subscription for authentication</summary>
    public string? PubSubServiceAccountEmail { get; set; }

    public bool HasServiceAccount => !String.IsNullOrWhiteSpace(this.ServiceAccountJson);
}


sealed class InAppPurchaseServerOptionsValidator : IValidateOptions<InAppPurchaseServerOptions>
{
    public ValidateOptionsResult Validate(string? name, InAppPurchaseServerOptions options)
    {
        var errors = new List<string>();
        if (options.Apple == null && options.Google == null)
            errors.Add("Configure at least one store - InAppPurchaseServerOptions.Apple and/or InAppPurchaseServerOptions.Google");

        if (options.Apple is { } apple)
        {
            if (String.IsNullOrWhiteSpace(apple.BundleId))
                errors.Add("Apple.BundleId is required");

            var creds = new[] { apple.IssuerId, apple.KeyId, apple.PrivateKey }.Count(x => !String.IsNullOrWhiteSpace(x));
            if (creds is > 0 and < 3)
            {
                errors.Add("Apple.IssuerId, Apple.KeyId and Apple.PrivateKey must all be set to use the App Store Server API");
            }
            else if (creds == 3)
            {
                try
                {
                    using var _ = AppleServerApiTokenProvider.LoadKey(apple.PrivateKey!);
                }
                catch (Exception ex) when (ex is ArgumentException or CryptographicException)
                {
                    errors.Add("Apple.PrivateKey is not a valid PEM EC private key (.p8): " + ex.Message);
                }
            }
        }

        if (options.Google is { } google)
        {
            if (String.IsNullOrWhiteSpace(google.PackageName))
                errors.Add("Google.PackageName is required");

            if (google.RequirePubSubAuthentication)
            {
                if (String.IsNullOrWhiteSpace(google.PubSubAudience))
                    errors.Add("Google.PubSubAudience is required when Google.RequirePubSubAuthentication is true");

                if (String.IsNullOrWhiteSpace(google.PubSubServiceAccountEmail))
                    errors.Add("Google.PubSubServiceAccountEmail is required when Google.RequirePubSubAuthentication is true");
            }

            if (google.HasServiceAccount)
            {
                try
                {
                    GoogleServiceAccount.Parse(google.ServiceAccountJson!);
                }
                catch (FormatException ex)
                {
                    errors.Add(ex.Message);
                }
            }
        }

        if (options.DeduplicationWindow <= TimeSpan.Zero)
            errors.Add("DeduplicationWindow must be positive");

        if (options.DeduplicationCapacity <= 0)
            errors.Add("DeduplicationCapacity must be positive");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
