using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.InAppPurchases.Server.Tests;


public record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? Body);


public sealed class StubHttpHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> responder) : HttpMessageHandler
{
    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        this.Requests.Enqueue(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body));
        return responder(request, body);
    }


    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}


public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, false);
}


static class B64
{
    public static string Url(string value) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(value));
    public static string Url(byte[] value) => Base64Url.EncodeToString(value);
    public static string Decode(string segment) => Encoding.UTF8.GetString(Base64Url.DecodeFromChars(segment));
}


/// <summary>
/// An in-test stand-in for Apple's PKI: root → intermediate (Apple WWDR OID) → leaf (App Store signing OID).
/// </summary>
public sealed class TestAppleChain : IDisposable
{
    const string LeafOid = "1.2.840.113635.100.6.11.1";
    const string IntermediateOid = "1.2.840.113635.100.6.2.1";

    readonly ECDsa rootKey;
    readonly ECDsa intermediateKey;
    readonly ECDsa leafKey;

    public X509Certificate2 Root { get; }
    public X509Certificate2 Intermediate { get; }
    public X509Certificate2 Leaf { get; }


    public TestAppleChain(bool leafOid = true, bool intermediateOid = true)
    {
        var notBefore = DateTimeOffset.UtcNow.AddDays(-30);
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);

        this.rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var rootReq = new CertificateRequest("CN=Test Apple Root CA, O=Test", this.rootKey, HashAlgorithmName.SHA256);
        rootReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        rootReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootReq.PublicKey, false));
        this.Root = rootReq.CreateSelfSigned(notBefore, notAfter);

        this.intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var intReq = new CertificateRequest("CN=Test Apple WWDR, O=Test", this.intermediateKey, HashAlgorithmName.SHA256);
        intReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        intReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        intReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(intReq.PublicKey, false));
        intReq.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(this.Root, true, false));
        if (intermediateOid)
            intReq.CertificateExtensions.Add(new X509Extension(new Oid(IntermediateOid), [0x05, 0x00], false));

        using (var intPublic = intReq.Create(this.Root, notBefore.AddDays(1), notAfter.AddDays(-1), Serial()))
            this.Intermediate = intPublic.CopyWithPrivateKey(this.intermediateKey);

        this.leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var leafReq = new CertificateRequest("CN=Test Prod ECC Mac App Store and iTunes Store Receipt Signing, O=Test", this.leafKey, HashAlgorithmName.SHA256);
        leafReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        leafReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        leafReq.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(this.Intermediate, true, false));
        if (leafOid)
            leafReq.CertificateExtensions.Add(new X509Extension(new Oid(LeafOid), [0x05, 0x00], false));

        using (var leafPublic = leafReq.Create(this.Intermediate, notBefore.AddDays(2), notAfter.AddDays(-2), Serial()))
            this.Leaf = leafPublic.CopyWithPrivateKey(this.leafKey);
    }


    public string Sign(string payloadJson, string alg = "ES256")
    {
        var header = new JsonObject
        {
            ["alg"] = alg,
            ["x5c"] = new JsonArray(
                Convert.ToBase64String(this.Leaf.RawData),
                Convert.ToBase64String(this.Intermediate.RawData),
                Convert.ToBase64String(this.Root.RawData)
            )
        };
        var input = B64.Url(header.ToJsonString()) + "." + B64.Url(payloadJson);
        var signature = this.leafKey.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return input + "." + B64.Url(signature);
    }


    static byte[] Serial() => RandomNumberGenerator.GetBytes(8);


    public void Dispose()
    {
        this.Leaf.Dispose();
        this.Intermediate.Dispose();
        this.Root.Dispose();
        this.leafKey.Dispose();
        this.intermediateKey.Dispose();
        this.rootKey.Dispose();
    }
}


static class ApplePayloads
{
    public const string BundleId = "com.test.app";
    public const long AppAppleId = 123456789;
    public static readonly Guid AccountToken = Guid.Parse("7e3fb20b-4cdb-47cc-936d-99d65f608138");


    static long Ms(DateTimeOffset date) => date.ToUnixTimeMilliseconds();


    public static string Transaction(
        string bundleId = BundleId,
        string environment = "Sandbox",
        string productId = "premium_monthly",
        DateTimeOffset? expires = null,
        long? revocationDate = null
    )
    {
        var now = DateTimeOffset.UtcNow;
        var json = new JsonObject
        {
            ["transactionId"] = "2000000123",
            ["originalTransactionId"] = "2000000100",
            ["webOrderLineItemId"] = "2000000050",
            ["bundleId"] = bundleId,
            ["productId"] = productId,
            ["subscriptionGroupIdentifier"] = "21000000",
            ["purchaseDate"] = Ms(now.AddMinutes(-5)),
            ["originalPurchaseDate"] = Ms(now.AddMonths(-2)),
            ["expiresDate"] = Ms(expires ?? now.AddMonths(1)),
            ["quantity"] = 1,
            ["type"] = "Auto-Renewable Subscription",
            ["appAccountToken"] = AccountToken.ToString(),
            ["inAppOwnershipType"] = "PURCHASED",
            ["signedDate"] = Ms(now),
            ["environment"] = environment,
            ["transactionReason"] = "PURCHASE",
            ["storefront"] = "USA",
            ["price"] = 4990,
            ["currency"] = "USD"
        };
        if (revocationDate != null)
            json["revocationDate"] = revocationDate;

        return json.ToJsonString();
    }


    public static string Renewal(string environment = "Sandbox") => new JsonObject
    {
        ["originalTransactionId"] = "2000000100",
        ["autoRenewProductId"] = "premium_monthly",
        ["productId"] = "premium_monthly",
        ["autoRenewStatus"] = 1,
        ["signedDate"] = Ms(DateTimeOffset.UtcNow),
        ["environment"] = environment,
        ["appAccountToken"] = AccountToken.ToString()
    }.ToJsonString();


    public static string Notification(
        TestAppleChain chain,
        string type = "SUBSCRIBED",
        string? subtype = "INITIAL_BUY",
        string? uuid = null,
        string bundleId = BundleId,
        string environment = "Sandbox",
        long? appAppleId = AppAppleId,
        bool includeRenewal = true
    )
    {
        var data = new JsonObject
        {
            ["bundleId"] = bundleId,
            ["bundleVersion"] = "1",
            ["environment"] = environment,
            ["signedTransactionInfo"] = chain.Sign(Transaction(bundleId, environment)),
            ["status"] = 1
        };
        if (appAppleId != null)
            data["appAppleId"] = appAppleId;

        if (includeRenewal)
            data["signedRenewalInfo"] = chain.Sign(Renewal(environment));

        var json = new JsonObject
        {
            ["notificationType"] = type,
            ["notificationUUID"] = uuid ?? Guid.NewGuid().ToString(),
            ["version"] = "2.0",
            ["signedDate"] = Ms(DateTimeOffset.UtcNow),
            ["data"] = data
        };
        if (subtype != null)
            json["subtype"] = subtype;

        return chain.Sign(json.ToJsonString());
    }
}


/// <summary>
/// Google OIDC token issuer + JWKS for Pub/Sub push authentication tests.
/// </summary>
public sealed class TestGoogleOidc : IDisposable
{
    public const string Audience = "https://example.com/iap/google";
    public const string Email = "pubsub-push@test-project.iam.gserviceaccount.com";
    public const string KeyId = "test-kid-1";

    readonly RSA key = RSA.Create(2048);
    public int JwksFetches;


    public string Jwks()
    {
        var p = this.key.ExportParameters(false);
        return new JsonObject
        {
            ["keys"] = new JsonArray(new JsonObject
            {
                ["kty"] = "RSA",
                ["alg"] = "RS256",
                ["use"] = "sig",
                ["kid"] = KeyId,
                ["n"] = B64.Url(p.Modulus!),
                ["e"] = B64.Url(p.Exponent!)
            })
        }.ToJsonString();
    }


    public HttpResponseMessage JwksResponse()
    {
        Interlocked.Increment(ref this.JwksFetches);
        var response = StubHttpHandler.Json(this.Jwks());
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { MaxAge = TimeSpan.FromHours(1), Public = true };
        return response;
    }


    public string Token(
        DateTimeOffset now,
        string audience = Audience,
        string email = Email,
        bool emailVerified = true,
        TimeSpan? lifetime = null,
        string kid = KeyId,
        string issuer = "https://accounts.google.com"
    )
    {
        var header = new JsonObject { ["alg"] = "RS256", ["kid"] = kid, ["typ"] = "JWT" };
        var claims = new JsonObject
        {
            ["iss"] = issuer,
            ["aud"] = audience,
            ["azp"] = "1234567890",
            ["email"] = email,
            ["email_verified"] = emailVerified,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.Add(lifetime ?? TimeSpan.FromHours(1)).ToUnixTimeSeconds(),
            ["sub"] = "1234567890"
        };
        var input = B64.Url(header.ToJsonString()) + "." + B64.Url(claims.ToJsonString());
        var signature = this.key.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return input + "." + B64.Url(signature);
    }


    public void Dispose() => this.key.Dispose();
}


public sealed class RecordingState
{
    public ConcurrentQueue<PurchaseEvent> Events { get; } = new();
    public int Attempts;
    public int FailuresRemaining;
}


public sealed class RecordingHandler(RecordingState state) : IPurchaseEventHandler
{
    public Task HandleAsync(PurchaseEvent purchaseEvent, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref state.Attempts);
        if (Interlocked.Decrement(ref state.FailuresRemaining) >= 0)
            throw new InvalidOperationException("Simulated handler failure");

        state.Events.Enqueue(purchaseEvent);
        return Task.CompletedTask;
    }
}


public sealed class InAppPurchaseTestHost : IAsyncDisposable
{
    readonly WebApplication app;

    public HttpClient Client { get; }
    public RecordingState State { get; }
    public IServiceProvider Services => this.app.Services;


    InAppPurchaseTestHost(WebApplication app)
    {
        this.app = app;
        this.Client = app.GetTestClient();
        this.State = app.Services.GetRequiredService<RecordingState>();
    }


    public static async Task<InAppPurchaseTestHost> StartAsync(
        Action<InAppPurchaseServerOptions> configure,
        HttpMessageHandler? stub = null,
        Action<IServiceCollection>? services = null
    )
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddSingleton<RecordingState>();
        builder.Services.AddInAppPurchaseServer(configure).AddPurchaseEventHandler<RecordingHandler>();
        if (stub != null)
            builder.Services.ConfigureHttpClientDefaults(x => x.ConfigurePrimaryHttpMessageHandler(() => stub));

        services?.Invoke(builder.Services);

        var app = builder.Build();
        app.MapInAppPurchaseWebhooks("/iap");
        app.MapInAppPurchaseVerification("/iap/verify");
        await app.StartAsync();
        return new InAppPurchaseTestHost(app);
    }


    public async ValueTask DisposeAsync()
    {
        this.Client.Dispose();
        await this.app.StopAsync();
        await this.app.DisposeAsync();
    }
}
