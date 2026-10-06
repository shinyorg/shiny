using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server.Tests;


public class AppleJwsVerificationTests : IDisposable
{
    readonly TestAppleChain chain = new();


    AppStoreClient Client(TestAppleChain? trusted = null, Action<AppleStoreOptions>? configure = null)
    {
        var apple = new AppleStoreOptions
        {
            BundleId = ApplePayloads.BundleId,
            AppAppleId = ApplePayloads.AppAppleId,
            TrustedRootsOverride = [(trusted ?? this.chain).Root]
        };
        configure?.Invoke(apple);

        return new AppStoreClient(
            new StubHttpClientFactory(new StubHttpHandler((_, _) => throw new InvalidOperationException("no http expected"))),
            Options.Create(new InAppPurchaseServerOptions { Apple = apple }),
            TimeProvider.System,
            NullLogger<AppStoreClient>.Instance
        );
    }


    [Fact]
    public void EmbeddedAppleRootCertificate_Loads()
    {
        using var root = AppleJwsVerifier.LoadAppleRootCertificate();
        Assert.Contains("Apple Root CA - G3", root.Subject);
        Assert.Equal("63343ABFB89A6A03EBB57E9B3F5FA7BE7C4F5C756F3017B3A8C488C3653E9179", root.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256));
    }


    [Fact]
    public void Notification_HappyPath_VerifiesAndDecodesNestedJws()
    {
        var client = this.Client();
        var uuid = Guid.NewGuid().ToString();
        var notification = client.VerifyNotification(ApplePayloads.Notification(this.chain, uuid: uuid));

        Assert.Equal("SUBSCRIBED", notification.NotificationType);
        Assert.Equal("INITIAL_BUY", notification.Subtype);
        Assert.Equal(uuid, notification.NotificationUuid);
        Assert.Equal(ApplePayloads.BundleId, notification.Data!.BundleId);

        var transaction = client.VerifyTransaction(notification.Data.SignedTransactionInfo!);
        Assert.Equal("premium_monthly", transaction.ProductId);
        Assert.Equal("2000000100", transaction.OriginalTransactionId);
        Assert.Equal(ApplePayloads.AccountToken.ToString(), transaction.AppAccountToken);
        Assert.True(transaction.IsSubscription);
        Assert.NotNull(transaction.ExpiresAt);

        var renewal = client.VerifyRenewalInfo(notification.Data.SignedRenewalInfo!);
        Assert.True(renewal.IsAutoRenewing);
    }


    [Fact]
    public void TamperedPayload_IsRejected()
    {
        var jws = this.chain.Sign(ApplePayloads.Transaction());
        var parts = jws.Split('.');
        var tampered = JsonNode.Parse(B64.Decode(parts[1]))!;
        tampered["productId"] = "lifetime_unlock";
        var forged = $"{parts[0]}.{B64.Url(tampered.ToJsonString())}.{parts[2]}";

        var ex = Assert.Throws<AppleVerificationException>(() => this.Client().VerifyTransaction(forged));
        Assert.Equal(AppleVerificationFailure.InvalidSignature, ex.Failure);
    }


    [Fact]
    public void UntrustedRoot_IsRejected()
    {
        using var other = new TestAppleChain();
        var ex = Assert.Throws<AppleVerificationException>(() => this.Client(trusted: other).VerifyTransaction(this.chain.Sign(ApplePayloads.Transaction())));
        Assert.Equal(AppleVerificationFailure.InvalidCertificateChain, ex.Failure);
    }


    [Fact]
    public void MissingLeafOid_IsRejected()
    {
        using var noOid = new TestAppleChain(leafOid: false);
        var ex = Assert.Throws<AppleVerificationException>(() => this.Client(trusted: noOid).VerifyTransaction(noOid.Sign(ApplePayloads.Transaction())));
        Assert.Equal(AppleVerificationFailure.InvalidCertificateChain, ex.Failure);
    }


    [Fact]
    public void MissingIntermediateOid_IsRejected()
    {
        using var noOid = new TestAppleChain(intermediateOid: false);
        var ex = Assert.Throws<AppleVerificationException>(() => this.Client(trusted: noOid).VerifyTransaction(noOid.Sign(ApplePayloads.Transaction())));
        Assert.Equal(AppleVerificationFailure.InvalidCertificateChain, ex.Failure);
    }


    [Theory]
    [InlineData("HS256")]
    [InlineData("none")]
    [InlineData("RS256")]
    public void WrongAlgorithm_IsRejected(string alg)
    {
        var ex = Assert.Throws<AppleVerificationException>(() => this.Client().VerifyTransaction(this.chain.Sign(ApplePayloads.Transaction(), alg)));
        Assert.Equal(AppleVerificationFailure.InvalidAlgorithm, ex.Failure);
    }


    [Fact]
    public void WrongBundleId_IsRejected()
    {
        var client = this.Client();
        var ex = Assert.Throws<AppleVerificationException>(() => client.VerifyTransaction(this.chain.Sign(ApplePayloads.Transaction(bundleId: "com.evil.app"))));
        Assert.Equal(AppleVerificationFailure.InvalidBundleId, ex.Failure);

        ex = Assert.Throws<AppleVerificationException>(() => client.VerifyNotification(ApplePayloads.Notification(this.chain, bundleId: "com.evil.app")));
        Assert.Equal(AppleVerificationFailure.InvalidBundleId, ex.Failure);
    }


    [Fact]
    public void ProductionNotification_RequiresMatchingAppAppleId()
    {
        var production = ApplePayloads.Notification(this.chain, environment: "Production");
        Assert.NotNull(this.Client().VerifyNotification(production));

        var ex = Assert.Throws<AppleVerificationException>(() => this.Client(configure: x => x.AppAppleId = null).VerifyNotification(production));
        Assert.Equal(AppleVerificationFailure.InvalidAppAppleId, ex.Failure);

        ex = Assert.Throws<AppleVerificationException>(() => this.Client(configure: x => x.AppAppleId = 42).VerifyNotification(production));
        Assert.Equal(AppleVerificationFailure.InvalidAppAppleId, ex.Failure);
    }


    [Fact]
    public void Sandbox_RejectedWhenNotAllowed()
    {
        var ex = Assert.Throws<AppleVerificationException>(() =>
            this.Client(configure: x => x.AllowSandbox = false).VerifyNotification(ApplePayloads.Notification(this.chain))
        );
        Assert.Equal(AppleVerificationFailure.InvalidEnvironment, ex.Failure);
    }


    [Theory]
    [InlineData("")]
    [InlineData("not-a-jws")]
    [InlineData("a.b")]
    [InlineData("!!!.@@@.###")]
    public void Garbage_IsInvalidFormat(string jws)
    {
        var ex = Assert.Throws<AppleVerificationException>(() => this.Client().VerifyTransaction(jws));
        Assert.Equal(AppleVerificationFailure.InvalidFormat, ex.Failure);
    }


    public void Dispose() => this.chain.Dispose();
}
