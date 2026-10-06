using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shiny.InAppPurchases.Server.Tests;


public class EndpointTests : IDisposable
{
    readonly TestAppleChain chain = new();
    readonly TestGoogleOidc oidc = new();
    readonly RSA serviceAccountKey = RSA.Create(2048);


    void ConfigureApple(InAppPurchaseServerOptions options) => options.Apple = new AppleStoreOptions
    {
        BundleId = ApplePayloads.BundleId,
        AppAppleId = ApplePayloads.AppAppleId,
        TrustedRootsOverride = [this.chain.Root]
    };


    void ConfigureGoogle(InAppPurchaseServerOptions options, bool auth = true, bool serviceAccount = true) => options.Google = new GooglePlayOptions
    {
        PackageName = "com.test.app",
        RequirePubSubAuthentication = auth,
        PubSubAudience = TestGoogleOidc.Audience,
        PubSubServiceAccountEmail = TestGoogleOidc.Email,
        ServiceAccountJson = serviceAccount ? GoogleFixtures.ServiceAccountJson(this.serviceAccountKey) : null
    };


    StubHttpHandler GoogleStub() => new((req, _) => req.RequestUri!.ToString() switch
    {
        "https://www.googleapis.com/oauth2/v3/certs" => this.oidc.JwksResponse(),
        "https://oauth2.googleapis.com/token" => StubHttpHandler.Json("""{"access_token":"ya29.test","expires_in":3600}"""),
        var u when u.Contains("/purchases/subscriptionsv2/tokens/") => StubHttpHandler.Json(GoogleFixtures.Subscription),
        var u when u.Contains("/purchases/productsv2/tokens/") => StubHttpHandler.Json(GoogleFixtures.Product),
        var u => throw new InvalidOperationException("Unexpected request " + u)
    });


    static StringContent AppleBody(string signedPayload)
        => new(new JsonObject { ["signedPayload"] = signedPayload }.ToJsonString(), Encoding.UTF8, "application/json");


    static StringContent PubSubBody(string notificationJson, string messageId = "136969346945") => new(new JsonObject
    {
        ["message"] = new JsonObject
        {
            ["attributes"] = new JsonObject(),
            ["data"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(notificationJson)),
            ["messageId"] = messageId,
            ["message_id"] = messageId,
            ["publishTime"] = "2030-04-12T20:04:05.000Z"
        },
        ["subscription"] = "projects/test-project/subscriptions/play-rtdn"
    }.ToJsonString(), Encoding.UTF8, "application/json");


    [Fact]
    public async Task Apple_Webhook_DispatchesNormalizedEvent()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var uuid = Guid.NewGuid().ToString();

        var response = await host.Client.PostAsync("/iap/apple", AppleBody(ApplePayloads.Notification(this.chain, uuid: uuid)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var e = Assert.Single(host.State.Events);
        Assert.Equal(uuid, e.NotificationId);
        Assert.Equal(StorePlatform.AppStore, e.Platform);
        Assert.Equal(PurchaseEventType.Purchased, e.Type);
        Assert.Equal("SUBSCRIBED/INITIAL_BUY", e.RawType);
        Assert.Equal("premium_monthly", e.ProductId);
        Assert.Equal("2000000123", e.TransactionId);
        Assert.Equal("2000000100", e.OriginalTransactionId);
        Assert.Equal(ApplePayloads.AccountToken, e.AccountToken);
        Assert.Equal(StoreEnvironment.Sandbox, e.Environment);
        Assert.True(e.IsAutoRenewing);
        Assert.NotNull(e.ExpiresAt);
        Assert.NotNull(e.Apple?.Transaction);
        Assert.NotNull(e.Apple?.RenewalInfo);
        Assert.Null(e.Google);
    }


    [Fact]
    public async Task Apple_DuplicateNotification_DispatchedOnce()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var payload = ApplePayloads.Notification(this.chain);

        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/iap/apple", AppleBody(payload))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/iap/apple", AppleBody(payload))).StatusCode);

        Assert.Single(host.State.Events);
        Assert.Equal(1, host.State.Attempts);
    }


    [Fact]
    public async Task Apple_HandlerThrows_Returns500_AndRedeliveryIsProcessed()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        host.State.FailuresRemaining = 1;
        var payload = ApplePayloads.Notification(this.chain);

        Assert.Equal(HttpStatusCode.InternalServerError, (await host.Client.PostAsync("/iap/apple", AppleBody(payload))).StatusCode);
        Assert.Empty(host.State.Events);

        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/iap/apple", AppleBody(payload))).StatusCode);
        Assert.Single(host.State.Events);
        Assert.Equal(2, host.State.Attempts);
    }


    [Fact]
    public async Task Apple_UntrustedSignature_Returns401()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        using var attacker = new TestAppleChain();

        var response = await host.Client.PostAsync("/iap/apple", AppleBody(ApplePayloads.Notification(attacker)));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(host.State.Events);
    }


    [Fact]
    public async Task Apple_WrongApp_Returns400()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var response = await host.Client.PostAsync("/iap/apple", AppleBody(ApplePayloads.Notification(this.chain, bundleId: "com.other.app")));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("not json")]
    public async Task Apple_MalformedBody_Returns400(string body)
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var response = await host.Client.PostAsync("/iap/apple", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task Apple_TestNotification_DispatchedAsTest()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var payload = this.chain.Sign(new JsonObject
        {
            ["notificationType"] = "TEST",
            ["notificationUUID"] = Guid.NewGuid().ToString(),
            ["version"] = "2.0",
            ["signedDate"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["data"] = new JsonObject { ["bundleId"] = ApplePayloads.BundleId, ["environment"] = "Sandbox" }
        }.ToJsonString());

        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/iap/apple", AppleBody(payload))).StatusCode);
        Assert.Equal(PurchaseEventType.Test, Assert.Single(host.State.Events).Type);
    }


    [Fact]
    public async Task Google_Push_Authenticated_DispatchesWithFetchedSubscription()
    {
        var stub = this.GoogleStub();
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o), stub);

        var request = new HttpRequestMessage(HttpMethod.Post, "/iap/google")
        {
            Content = PubSubBody("""
            {"version":"1.0","packageName":"com.test.app","eventTimeMillis":"1903000000000",
             "subscriptionNotification":{"version":"1.0","notificationType":4,"purchaseToken":"tok-1","subscriptionId":"premium_monthly"}}
            """)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.oidc.Token(DateTimeOffset.UtcNow));

        var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var e = Assert.Single(host.State.Events);
        Assert.Equal("136969346945", e.NotificationId);
        Assert.Equal(StorePlatform.GooglePlay, e.Platform);
        Assert.Equal(PurchaseEventType.Purchased, e.Type);
        Assert.Equal("SUBSCRIPTION_PURCHASED", e.RawType);
        Assert.Equal("premium_monthly", e.ProductId);
        Assert.Equal("tok-1", e.OriginalTransactionId);
        Assert.Equal("GPA.3333-4444-5555-66666..1", e.TransactionId);
        Assert.Equal(GoogleFixtures.AccountToken, e.AccountToken);
        Assert.Equal(StoreEnvironment.Sandbox, e.Environment);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1903000000000), e.OccurredAt);
        Assert.True(e.IsAutoRenewing);
        Assert.NotNull(e.ExpiresAt);
        Assert.NotNull(e.Google?.Subscription);
        Assert.Equal("projects/test-project/subscriptions/play-rtdn", e.Google!.PubSubSubscription);
        Assert.Contains(stub.Requests, x => x.Uri.ToString().EndsWith("/purchases/subscriptionsv2/tokens/tok-1") && x.Authorization == "Bearer ya29.test");
    }


    [Fact]
    public async Task Google_OneTimeProduct_DispatchesWithFetchedProduct()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o, auth: false), this.GoogleStub());

        var response = await host.Client.PostAsync("/iap/google", PubSubBody("""
            {"version":"1.0","packageName":"com.test.app","eventTimeMillis":1903000000000,
             "oneTimeProductNotification":{"version":"1.0","notificationType":1,"purchaseToken":"tok-2","sku":"coins_100"}}
            """));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var e = Assert.Single(host.State.Events);
        Assert.Equal(PurchaseEventType.Purchased, e.Type);
        Assert.Equal("coins_100", e.ProductId);
        Assert.Equal("GPA.1111-2222-3333-44444", e.TransactionId);
        Assert.Equal(3, e.Quantity);
        Assert.NotNull(e.Google?.Product);
    }


    [Fact]
    public async Task Google_MissingOrInvalidToken_Returns401()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o), this.GoogleStub());
        var body = """{"version":"1.0","packageName":"com.test.app","testNotification":{"version":"1.0"}}""";

        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsync("/iap/google", PubSubBody(body))).StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Post, "/iap/google") { Content = PubSubBody(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.oidc.Token(DateTimeOffset.UtcNow, audience: "https://wrong"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.SendAsync(request)).StatusCode);

        Assert.Empty(host.State.Events);
    }


    [Fact]
    public async Task Google_TestNotification_DispatchedAsTest_WithoutApiCalls()
    {
        var stub = this.GoogleStub();
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o, auth: false, serviceAccount: false), stub);

        var response = await host.Client.PostAsync("/iap/google", PubSubBody("""{"version":"1.0","packageName":"com.test.app","eventTimeMillis":"1903000000000","testNotification":{"version":"1.0"}}"""));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var e = Assert.Single(host.State.Events);
        Assert.Equal(PurchaseEventType.Test, e.Type);
        Assert.Equal(StoreEnvironment.Unknown, e.Environment);
        Assert.Empty(stub.Requests);
    }


    [Fact]
    public async Task Google_WrongPackage_Returns400()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o, auth: false, serviceAccount: false));
        var response = await host.Client.PostAsync("/iap/google", PubSubBody("""{"version":"1.0","packageName":"com.other.app","testNotification":{"version":"1.0"}}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task Google_DuplicateMessage_DispatchedOnce()
    {
        var stub = this.GoogleStub();
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o, auth: false), stub);
        var body = """{"version":"1.0","packageName":"com.test.app","subscriptionNotification":{"version":"1.0","notificationType":2,"purchaseToken":"tok-3"}}""";

        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/iap/google", PubSubBody(body, "m-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/iap/google", PubSubBody(body, "m-1"))).StatusCode);

        Assert.Single(host.State.Events);
        Assert.Single(stub.Requests, x => x.Uri.ToString().Contains("subscriptionsv2"));
    }


    [Fact]
    public async Task Store_NotConfigured_Returns404()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsync("/iap/google", PubSubBody("{}"))).StatusCode);
    }


    [Fact]
    public async Task Verify_AppleTransaction()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);

        var response = await host.Client.PostAsync("/iap/verify", JsonContent(new JsonObject
        {
            ["platform"] = "AppStore",
            ["verificationData"] = this.chain.Sign(ApplePayloads.Transaction()),
            ["productId"] = "premium_monthly"
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var r = doc.RootElement;
        Assert.True(r.GetProperty("isValid").GetBoolean());
        Assert.True(r.GetProperty("isActive").GetBoolean());
        Assert.Equal("Subscription", r.GetProperty("kind").GetString());
        Assert.Equal("Sandbox", r.GetProperty("environment").GetString());
        Assert.Equal(ApplePayloads.AccountToken, r.GetProperty("accountToken").GetGuid());
    }


    [Fact]
    public async Task Verify_AppleForgedOrMismatched_IsInvalid()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var verifier = host.Services.GetRequiredService<IPurchaseVerifier>();
        using var attacker = new TestAppleChain();

        var forged = await verifier.VerifyAsync(new PurchaseVerificationRequest { Platform = StorePlatform.AppStore, VerificationData = attacker.Sign(ApplePayloads.Transaction()) });
        Assert.False(forged.IsValid);

        var mismatch = await verifier.VerifyAsync(new PurchaseVerificationRequest { Platform = StorePlatform.AppStore, VerificationData = this.chain.Sign(ApplePayloads.Transaction()), ProductId = "lifetime" });
        Assert.False(mismatch.IsValid);

        var revoked = await verifier.VerifyAsync(new PurchaseVerificationRequest
        {
            Platform = StorePlatform.AppStore,
            VerificationData = this.chain.Sign(ApplePayloads.Transaction(revocationDate: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
        });
        Assert.True(revoked.IsValid);
        Assert.False(revoked.IsActive);
    }


    [Fact]
    public async Task Verify_GoogleOneTimeProduct_FallsBackFromSubscriptionLookup()
    {
        var stub = new StubHttpHandler((req, _) => req.RequestUri!.ToString() switch
        {
            "https://oauth2.googleapis.com/token" => StubHttpHandler.Json("""{"access_token":"ya29.test","expires_in":3600}"""),
            var u when u.Contains("subscriptionsv2") => StubHttpHandler.Json("""{"error":{"code":400,"message":"Invalid Value"}}""", HttpStatusCode.BadRequest),
            var u when u.Contains("productsv2") => StubHttpHandler.Json(GoogleFixtures.Product),
            var u => throw new InvalidOperationException(u)
        });
        await using var host = await InAppPurchaseTestHost.StartAsync(o => this.ConfigureGoogle(o, auth: false), stub);

        var result = await host.Services.GetRequiredService<IPurchaseVerifier>().VerifyAsync(new PurchaseVerificationRequest
        {
            Platform = StorePlatform.GooglePlay,
            VerificationData = "tok-product",
            ProductId = "coins_100"
        });

        Assert.True(result.IsValid);
        Assert.True(result.IsActive);
        Assert.Equal(PurchaseKind.OneTime, result.Kind);
        Assert.False(result.IsAcknowledged);
        Assert.Equal(3, result.Quantity);
        Assert.Equal(StoreEnvironment.Production, result.Environment);
        Assert.Equal(GoogleFixtures.AccountToken, result.AccountToken);
    }


    [Fact]
    public async Task Verify_MalformedRequest_Returns400()
    {
        await using var host = await InAppPurchaseTestHost.StartAsync(this.ConfigureApple);
        var response = await host.Client.PostAsync("/iap/verify", new StringContent("""{"platform":"Windows","verificationData":"x"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task InvalidOptions_FailAtStartup()
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() => InAppPurchaseTestHost.StartAsync(_ => { }));
        await Assert.ThrowsAsync<OptionsValidationException>(() => InAppPurchaseTestHost.StartAsync(o => o.Google = new GooglePlayOptions { PackageName = "com.test.app" }));
        await Assert.ThrowsAsync<OptionsValidationException>(() => InAppPurchaseTestHost.StartAsync(o => o.Apple = new AppleStoreOptions { BundleId = "b", IssuerId = "i", KeyId = "k", PrivateKey = "not a key" }));
    }


    static StringContent JsonContent(JsonObject json) => new(json.ToJsonString(), Encoding.UTF8, "application/json");


    public void Dispose()
    {
        this.chain.Dispose();
        this.oidc.Dispose();
        this.serviceAccountKey.Dispose();
    }
}
