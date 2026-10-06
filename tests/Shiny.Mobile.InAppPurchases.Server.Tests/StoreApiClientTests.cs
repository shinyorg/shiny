using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server.Tests;


public class StoreApiClientTests
{
    [Fact]
    public void AppleServerApiToken_HasExpectedShape_AndIsCached()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var apple = new AppleStoreOptions
        {
            BundleId = "com.test.app",
            IssuerId = "57246542-96fe-1a63-e053-0824d011072a",
            KeyId = "2X9R4HXF34",
            PrivateKey = key.ExportPkcs8PrivateKeyPem()
        };
        var provider = new AppleServerApiTokenProvider(apple, time);

        var token = provider.GetToken();
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        using var header = JsonDocument.Parse(B64.Decode(parts[0]));
        Assert.Equal("ES256", header.RootElement.GetProperty("alg").GetString());
        Assert.Equal("2X9R4HXF34", header.RootElement.GetProperty("kid").GetString());
        Assert.Equal("JWT", header.RootElement.GetProperty("typ").GetString());

        using var claims = JsonDocument.Parse(B64.Decode(parts[1]));
        var c = claims.RootElement;
        Assert.Equal(apple.IssuerId, c.GetProperty("iss").GetString());
        Assert.Equal("appstoreconnect-v1", c.GetProperty("aud").GetString());
        Assert.Equal("com.test.app", c.GetProperty("bid").GetString());
        var lifetime = c.GetProperty("exp").GetInt64() - c.GetProperty("iat").GetInt64();
        Assert.InRange(lifetime, 60, 3600);

        var signature = System.Buffers.Text.Base64Url.DecodeFromChars(parts[2]);
        Assert.True(key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));

        Assert.Equal(token, provider.GetToken());
        time.Advance(TimeSpan.FromMinutes(30));
        Assert.NotEqual(token, provider.GetToken());
    }


    [Fact]
    public async Task AppleGetTransaction_FallsBackToSandboxOn404()
    {
        using var chain = new TestAppleChain();
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var signed = chain.Sign(ApplePayloads.Transaction());

        var stub = new StubHttpHandler((req, _) => req.RequestUri!.Host switch
        {
            "api.storekit.itunes.apple.com" => StubHttpHandler.Json("""{"errorCode":4040010,"errorMessage":"Transaction id not found."}""", HttpStatusCode.NotFound),
            "api.storekit-sandbox.itunes.apple.com" => StubHttpHandler.Json(new JsonObject { ["signedTransactionInfo"] = signed }.ToJsonString()),
            _ => throw new InvalidOperationException(req.RequestUri.ToString())
        });

        var client = new AppStoreClient(
            new StubHttpClientFactory(stub),
            Options.Create(new InAppPurchaseServerOptions
            {
                Apple = new AppleStoreOptions
                {
                    BundleId = ApplePayloads.BundleId,
                    IssuerId = "issuer",
                    KeyId = "KEY",
                    PrivateKey = key.ExportPkcs8PrivateKeyPem(),
                    TrustedRootsOverride = [chain.Root]
                }
            }),
            TimeProvider.System,
            NullLogger<AppStoreClient>.Instance
        );

        var transaction = await client.GetTransactionAsync("2000000123");
        Assert.NotNull(transaction);
        Assert.Equal("premium_monthly", transaction.ProductId);

        var requests = stub.Requests.ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Equal("https://api.storekit.itunes.apple.com/inApps/v1/transactions/2000000123", requests[0].Uri.ToString());
        Assert.StartsWith("https://api.storekit-sandbox.itunes.apple.com/", requests[1].Uri.ToString());
        Assert.All(requests, r => Assert.StartsWith("Bearer ey", r.Authorization));
    }


    [Fact]
    public async Task AppleApiError_Throws()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var stub = new StubHttpHandler((_, _) => StubHttpHandler.Json("""{"errorCode":4290000,"errorMessage":"Rate limit exceeded."}""", HttpStatusCode.TooManyRequests));
        var client = new AppStoreClient(
            new StubHttpClientFactory(stub),
            Options.Create(new InAppPurchaseServerOptions { Apple = new() { BundleId = "b", IssuerId = "i", KeyId = "k", PrivateKey = key.ExportPkcs8PrivateKeyPem() } }),
            TimeProvider.System,
            NullLogger<AppStoreClient>.Instance
        );

        var ex = await Assert.ThrowsAsync<AppleStoreApiException>(() => client.GetTransactionAsync("1"));
        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(4290000, ex.ErrorCode);
    }


    [Fact]
    public async Task GoogleServiceAccount_ExchangesSignedAssertion_AndFetchesSubscription()
    {
        using var rsa = RSA.Create(2048);
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var serviceAccount = GoogleFixtures.ServiceAccountJson(rsa);

        var stub = new StubHttpHandler((req, body) =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://oauth2.googleapis.com/token")
            {
                var form = body!.Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1]));
                Assert.Equal("urn:ietf:params:oauth:grant-type:jwt-bearer", form["grant_type"]);

                var parts = form["assertion"].Split('.');
                Assert.True(rsa.VerifyData(
                    Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]),
                    System.Buffers.Text.Base64Url.DecodeFromChars(parts[2]),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                ));

                using var header = JsonDocument.Parse(B64.Decode(parts[0]));
                Assert.Equal("RS256", header.RootElement.GetProperty("alg").GetString());
                Assert.Equal("key-id-1", header.RootElement.GetProperty("kid").GetString());

                using var claims = JsonDocument.Parse(B64.Decode(parts[1]));
                Assert.Equal(GoogleFixtures.ClientEmail, claims.RootElement.GetProperty("iss").GetString());
                Assert.Equal("https://www.googleapis.com/auth/androidpublisher", claims.RootElement.GetProperty("scope").GetString());
                Assert.Equal("https://oauth2.googleapis.com/token", claims.RootElement.GetProperty("aud").GetString());

                return StubHttpHandler.Json("""{"access_token":"ya29.test-token","expires_in":3599,"token_type":"Bearer"}""");
            }

            Assert.Equal("Bearer ya29.test-token", req.Headers.Authorization!.ToString());
            Assert.Equal("https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.test.app/purchases/subscriptionsv2/tokens/token%2F1", uri);
            return StubHttpHandler.Json(GoogleFixtures.Subscription);
        });

        var options = Options.Create(new InAppPurchaseServerOptions { Google = new GooglePlayOptions { PackageName = "com.test.app", ServiceAccountJson = serviceAccount } });
        var factory = new StubHttpClientFactory(stub);
        using var tokens = new GoogleAccessTokenProvider(factory, options, time);
        var client = new GooglePlayClient(factory, options, tokens);

        var subscription = await client.GetSubscriptionAsync("token/1");
        Assert.NotNull(subscription);
        Assert.Equal("SUBSCRIPTION_STATE_ACTIVE", subscription.SubscriptionState);
        Assert.True(subscription.IsTestPurchase);
        Assert.True(subscription.IsAcknowledged);
        Assert.Equal(GoogleFixtures.AccountToken.ToString("N"), subscription.ExternalAccountIdentifiers!.ObfuscatedExternalAccountId);

        var line = Assert.Single(subscription.LineItems!);
        Assert.Equal("premium_monthly", line.ProductId);
        Assert.Equal(new DateTimeOffset(2030, 5, 12, 20, 4, 4, TimeSpan.Zero).AddTicks(5211234), line.ExpiryTime);
        Assert.True(line.AutoRenewingPlan!.AutoRenewEnabled);

        await client.GetSubscriptionAsync("token/1");
        Assert.Equal(1, stub.Requests.Count(x => x.Uri.Host == "oauth2.googleapis.com"));

        time.Advance(TimeSpan.FromHours(1));
        await client.GetSubscriptionAsync("token/1");
        Assert.Equal(2, stub.Requests.Count(x => x.Uri.Host == "oauth2.googleapis.com"));
    }


    [Fact]
    public async Task GoogleNotFound_ReturnsNull_OtherErrorsThrow()
    {
        using var rsa = RSA.Create(2048);
        var status = HttpStatusCode.NotFound;
        var stub = new StubHttpHandler((req, _) => req.RequestUri!.Host == "oauth2.googleapis.com"
            ? StubHttpHandler.Json("""{"access_token":"t","expires_in":3600}""")
            : StubHttpHandler.Json("""{"error":{"code":500,"message":"backend error"}}""", status)
        );
        var options = Options.Create(new InAppPurchaseServerOptions { Google = new GooglePlayOptions { PackageName = "com.test.app", ServiceAccountJson = GoogleFixtures.ServiceAccountJson(rsa) } });
        var factory = new StubHttpClientFactory(stub);
        using var tokens = new GoogleAccessTokenProvider(factory, options, TimeProvider.System);
        var client = new GooglePlayClient(factory, options, tokens);

        Assert.Null(await client.GetProductAsync("missing"));

        status = HttpStatusCode.InternalServerError;
        var ex = await Assert.ThrowsAsync<GooglePlayApiException>(() => client.GetProductAsync("broken"));
        Assert.Contains("backend error", ex.Message);
    }


    [Fact]
    public async Task GoogleAcknowledge_PostsToExpectedUrl()
    {
        using var rsa = RSA.Create(2048);
        var stub = new StubHttpHandler((req, _) => req.RequestUri!.Host == "oauth2.googleapis.com"
            ? StubHttpHandler.Json("""{"access_token":"t","expires_in":3600}""")
            : StubHttpHandler.Json("")
        );
        var options = Options.Create(new InAppPurchaseServerOptions { Google = new GooglePlayOptions { PackageName = "com.test.app", ServiceAccountJson = GoogleFixtures.ServiceAccountJson(rsa) } });
        var factory = new StubHttpClientFactory(stub);
        using var tokens = new GoogleAccessTokenProvider(factory, options, TimeProvider.System);
        var client = new GooglePlayClient(factory, options, tokens);

        await client.AcknowledgeSubscriptionAsync("premium_monthly", "tok");
        await client.AcknowledgeProductAsync("remove_ads", "tok");
        await client.ConsumeProductAsync("coins_100", "tok");

        var urls = stub.Requests.Where(x => x.Uri.Host != "oauth2.googleapis.com").Select(x => x.Method + " " + x.Uri).ToArray();
        Assert.Equal(
            [
                "POST https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.test.app/purchases/subscriptions/premium_monthly/tokens/tok:acknowledge",
                "POST https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.test.app/purchases/products/remove_ads/tokens/tok:acknowledge",
                "POST https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.test.app/purchases/products/coins_100/tokens/tok:consume"
            ],
            urls
        );
    }
}


static class GoogleFixtures
{
    public const string ClientEmail = "play-api@test-project.iam.gserviceaccount.com";
    public static readonly Guid AccountToken = Guid.Parse("3b1a2c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d");


    public static string ServiceAccountJson(RSA rsa) => new JsonObject
    {
        ["type"] = "service_account",
        ["project_id"] = "test-project",
        ["private_key_id"] = "key-id-1",
        ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(),
        ["client_email"] = ClientEmail,
        ["token_uri"] = "https://oauth2.googleapis.com/token"
    }.ToJsonString();


    public static readonly string Subscription = $$"""
    {
      "kind": "androidpublisher#subscriptionPurchaseV2",
      "regionCode": "US",
      "startTime": "2030-04-12T20:04:04.521Z",
      "subscriptionState": "SUBSCRIPTION_STATE_ACTIVE",
      "latestOrderId": "GPA.3333-4444-5555-66666",
      "acknowledgementState": "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED",
      "externalAccountIdentifiers": { "obfuscatedExternalAccountId": "{{AccountToken:N}}" },
      "testPurchase": {},
      "lineItems": [
        {
          "productId": "premium_monthly",
          "expiryTime": "2030-05-12T20:04:04.521123456Z",
          "latestSuccessfulOrderId": "GPA.3333-4444-5555-66666..1",
          "autoRenewingPlan": { "autoRenewEnabled": true, "recurringPrice": { "currencyCode": "USD", "units": "4", "nanos": 990000000 } },
          "offerDetails": { "basePlanId": "monthly", "offerTags": [] }
        }
      ]
    }
    """;


    public static readonly string Product = $$"""
    {
      "kind": "androidpublisher#productPurchaseV2",
      "orderId": "GPA.1111-2222-3333-44444",
      "regionCode": "US",
      "purchaseCompletionTime": "2030-04-12T20:04:04Z",
      "acknowledgementState": "ACKNOWLEDGEMENT_STATE_PENDING",
      "obfuscatedExternalAccountId": "{{AccountToken:N}}",
      "purchaseStateContext": { "purchaseState": "PURCHASED" },
      "productLineItem": [
        { "productId": "coins_100", "productOfferDetails": { "quantity": 3, "refundableQuantity": 3, "consumptionState": "CONSUMPTION_STATE_YET_TO_BE_CONSUMED" } }
      ]
    }
    """;
}
