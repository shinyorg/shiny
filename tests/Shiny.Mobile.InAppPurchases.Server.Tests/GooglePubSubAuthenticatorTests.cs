using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server.Tests;


public class GooglePubSubAuthenticatorTests : IDisposable
{
    readonly TestGoogleOidc oidc = new();
    readonly FakeTimeProvider time = new(DateTimeOffset.UtcNow);
    readonly GooglePubSubAuthenticator authenticator;
    readonly GooglePlayOptions options = new()
    {
        PackageName = "com.test.app",
        PubSubAudience = TestGoogleOidc.Audience,
        PubSubServiceAccountEmail = TestGoogleOidc.Email
    };


    public GooglePubSubAuthenticatorTests()
    {
        var stub = new StubHttpHandler((req, _) =>
        {
            Assert.Equal(GooglePubSubAuthenticator.JwksUrl, req.RequestUri!.ToString());
            return this.oidc.JwksResponse();
        });
        this.authenticator = new GooglePubSubAuthenticator(new StubHttpClientFactory(stub), this.time, NullLogger<GooglePubSubAuthenticator>.Instance);
    }


    Task<string?> Validate(string? token) => this.authenticator.ValidateAsync(token == null ? null : "Bearer " + token, this.options, CancellationToken.None);


    [Fact]
    public async Task ValidToken_Accepted()
        => Assert.Null(await this.Validate(this.oidc.Token(this.time.GetUtcNow())));


    [Fact]
    public async Task LegacyIssuer_Accepted()
        => Assert.Null(await this.Validate(this.oidc.Token(this.time.GetUtcNow(), issuer: "accounts.google.com")));


    [Fact]
    public async Task ExpiredToken_Rejected()
        => Assert.Equal("Token expired", await this.Validate(this.oidc.Token(this.time.GetUtcNow().AddHours(-3))));


    [Fact]
    public async Task WrongAudience_Rejected()
        => Assert.Equal("Invalid audience", await this.Validate(this.oidc.Token(this.time.GetUtcNow(), audience: "https://attacker.example/iap/google")));


    [Fact]
    public async Task WrongEmail_Rejected()
        => Assert.Equal("Invalid service account email", await this.Validate(this.oidc.Token(this.time.GetUtcNow(), email: "someone@else.iam.gserviceaccount.com")));


    [Fact]
    public async Task UnverifiedEmail_Rejected()
        => Assert.NotNull(await this.Validate(this.oidc.Token(this.time.GetUtcNow(), emailVerified: false)));


    [Fact]
    public async Task WrongIssuer_Rejected()
        => Assert.Equal("Invalid issuer", await this.Validate(this.oidc.Token(this.time.GetUtcNow(), issuer: "https://evil.example")));


    [Fact]
    public async Task UnknownKid_Rejected_AndRefetchIsRateLimited()
    {
        Assert.Null(await this.Validate(this.oidc.Token(this.time.GetUtcNow())));
        Assert.Equal(1, this.oidc.JwksFetches);

        Assert.Equal("Unknown signing key", await this.Validate(this.oidc.Token(this.time.GetUtcNow(), kid: "rotated")));
        Assert.Equal("Unknown signing key", await this.Validate(this.oidc.Token(this.time.GetUtcNow(), kid: "rotated")));
        Assert.Equal(1, this.oidc.JwksFetches);

        this.time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal("Unknown signing key", await this.Validate(this.oidc.Token(this.time.GetUtcNow(), kid: "rotated")));
        Assert.Equal(2, this.oidc.JwksFetches);
    }


    [Fact]
    public async Task ForgedSignature_Rejected()
    {
        using var attacker = new TestGoogleOidc();
        Assert.Equal("Invalid signature", await this.Validate(attacker.Token(this.time.GetUtcNow())));
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a.b.c")]
    public async Task Malformed_Rejected(string? token)
        => Assert.NotNull(await this.Validate(token));


    [Fact]
    public async Task KeysCached_UntilMaxAge()
    {
        await this.Validate(this.oidc.Token(this.time.GetUtcNow()));
        await this.Validate(this.oidc.Token(this.time.GetUtcNow()));
        Assert.Equal(1, this.oidc.JwksFetches);

        this.time.Advance(TimeSpan.FromHours(2));
        Assert.Null(await this.Validate(this.oidc.Token(this.time.GetUtcNow())));
        Assert.Equal(2, this.oidc.JwksFetches);
    }


    public void Dispose() => this.oidc.Dispose();
}
