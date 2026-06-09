using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class AwsS3UploadRequestTests
{
    // Fixed wall-clock used for every signing-vector test — picks a date well outside the
    // current DateTime.UtcNow so we know the assertions aren't accidentally matching today's date.
    static readonly DateTimeOffset FixedClock = new(2024, 5, 24, 12, 34, 56, TimeSpan.Zero);


    static AwsS3UploadRequest NewSignedRequest(string filePath, TimeProvider clock)
    {
        var req = new AwsS3UploadRequest(filePath) { TimeProvider = clock }
            .WithBucket("my-bucket", "us-east-1")
            .WithCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        return req;
    }


    // -------- Fluent builder coverage --------

    [Fact]
    public void Fluent_AllSetters_PopulateProperties()
    {
        var req = new AwsS3UploadRequest("/tmp/x.bin")
            .WithBucket("b", "r")
            .WithObjectKey("k")
            .WithCustomUri("https://example.com/u")
            .WithMeteredConnection()
            .WithHeader("X-Custom", "v")
            .WithContentType("text/plain")
            .WithStorageClass("GLACIER")
            .WithCredentials("ak", "sk", "tok");

        Assert.Equal("b", req.BucketName);
        Assert.Equal("r", req.Region);
        Assert.Equal("k", req.ObjectKey);
        Assert.Equal("https://example.com/u", req.CustomUri);
        Assert.True(req.UseMeteredConnection);
        Assert.Equal("v", req.Headers["X-Custom"]);
        Assert.Equal("text/plain", req.ContentType);
        Assert.Equal("GLACIER", req.StorageClass);
        Assert.Equal("ak", req.AccessKeyId);
        Assert.Equal("sk", req.SecretAccessKey);
        Assert.Equal("tok", req.SessionToken);
    }


    [Fact]
    public void WithPresignedUrl_SetsField()
    {
        var req = new AwsS3UploadRequest("/tmp/x.bin").WithPresignedUrl("https://signed");
        Assert.Equal("https://signed", req.PresignedUrl);
    }


    // -------- Build() validation --------

    [Fact]
    public void Build_WithoutAuth_Throws()
    {
        using var f = new TempFile();
        var req = new AwsS3UploadRequest(f.Path);
        Assert.Throws<InvalidOperationException>(() => req.Build());
    }


    [Fact]
    public void Build_WithCredentials_NoBucket_Throws()
    {
        using var f = new TempFile();
        var req = new AwsS3UploadRequest(f.Path).WithCredentials("ak", "sk");
        Assert.Throws<InvalidOperationException>(() => req.Build());
    }


    [Fact]
    public void Build_WithCredentials_NoRegion_Throws()
    {
        using var f = new TempFile();
        var req = new AwsS3UploadRequest(f.Path).WithCredentials("ak", "sk");
        req.BucketName = "b";
        Assert.Throws<InvalidOperationException>(() => req.Build());
    }


    [Fact]
    public void Build_FileMissing_Throws()
    {
        var req = new AwsS3UploadRequest("/tmp/does-not-exist-" + Guid.NewGuid())
            .WithBucket("b", "r")
            .WithCredentials("ak", "sk");
        Assert.Throws<FileNotFoundException>(() => req.Build());
    }


    // -------- Generated request shape --------

    [Fact]
    public void Build_PresignedUrl_UsesUrlAsIs_AddsContentLength()
    {
        using var f = new TempFile(bytes: 512);
        var transfer = new AwsS3UploadRequest(f.Path)
            .WithPresignedUrl("https://example.com/signed")
            .Build();

        Assert.Equal("https://example.com/signed", transfer.Uri);
        Assert.Equal("PUT", transfer.HttpMethod);
        Assert.Equal(TransferType.UploadRaw, transfer.Type);
        Assert.Equal("512", transfer.Headers["Content-Length"]);
    }


    [Fact]
    public void Build_StandardUriShape_BucketRegionHostObjectKey()
    {
        using var f = new TempFile();
        var transfer = new AwsS3UploadRequest(f.Path) { TimeProvider = new FakeTimeProvider(FixedClock) }
            .WithBucket("my-bucket", "us-west-2")
            .WithObjectKey("folder/file.txt")
            .WithCredentials("AK", "SK")
            .Build();

        // Object key path components are URL-escaped (per builder logic), so the / between
        // folder and file is preserved but the segments are escaped.
        Assert.Equal("https://my-bucket.s3.us-west-2.amazonaws.com/folder%2Ffile.txt", transfer.Uri);
    }


    [Fact]
    public void Build_CustomUriWins_OverStandardComposition()
    {
        using var f = new TempFile();
        var transfer = new AwsS3UploadRequest(f.Path) { TimeProvider = new FakeTimeProvider(FixedClock) }
            .WithBucket("b", "us-east-1")
            .WithCustomUri("https://minio.local/raw")
            .WithCredentials("ak", "sk")
            .Build();

        Assert.Equal("https://minio.local/raw", transfer.Uri);
    }


    [Fact]
    public void Build_ObjectKey_DefaultsToFileName()
    {
        using var f = new TempFile();
        var fileName = Path.GetFileName(f.Path);

        var transfer = new AwsS3UploadRequest(f.Path) { TimeProvider = new FakeTimeProvider(FixedClock) }
            .WithBucket("b", "us-east-1")
            .WithCredentials("ak", "sk")
            .Build();

        Assert.EndsWith(Uri.EscapeDataString(fileName), transfer.Uri);
    }


    // -------- Required signed headers --------

    [Fact]
    public void Sign_PopulatesRequiredHeaders()
    {
        using var f = new TempFile(bytes: 100);
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));

        req.Build();

        Assert.Equal("my-bucket.s3.us-east-1.amazonaws.com", req.Headers["Host"]);
        Assert.Equal("UNSIGNED-PAYLOAD", req.Headers["x-amz-content-sha256"]);
        Assert.Equal("application/octet-stream", req.Headers["Content-Type"]);
        Assert.Equal("100", req.Headers["Content-Length"]);
        Assert.True(req.Headers.ContainsKey("x-amz-date"));
        Assert.True(req.Headers.ContainsKey("Authorization"));
    }


    [Fact]
    public void Sign_UsesInjectedClock_ForAmzDate()
    {
        using var f = new TempFile();
        var clock = new FakeTimeProvider(FixedClock);

        NewSignedRequest(f.Path, clock).Build();

        // 2024-05-24T12:34:56Z → 20240524T123456Z
        var req = NewSignedRequest(f.Path, clock);
        req.Build();
        Assert.Equal("20240524T123456Z", req.Headers["x-amz-date"]);
    }


    [Fact]
    public void Sign_AddsSessionToken_WhenProvided()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));
        req.SessionToken = "tok-1234";

        req.Build();

        Assert.Equal("tok-1234", req.Headers["x-amz-security-token"]);
    }


    [Fact]
    public void Sign_AddsStorageClass_WhenProvided()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));
        req.StorageClass = "STANDARD_IA";

        req.Build();

        Assert.Equal("STANDARD_IA", req.Headers["x-amz-storage-class"]);
    }


    [Fact]
    public void Sign_ContentType_OverrideHonored()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));
        req.ContentType = "image/png";

        req.Build();

        Assert.Equal("image/png", req.Headers["Content-Type"]);
    }


    // -------- Authorization header structure --------

    [Fact]
    public void Auth_HeaderFollowsAwsV4Format()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));

        req.Build();
        var auth = req.Headers["Authorization"];

        Assert.Matches(new Regex(@"^AWS4-HMAC-SHA256 Credential=[^,]+/\d{8}/us-east-1/s3/aws4_request, SignedHeaders=[^,]+, Signature=[a-f0-9]{64}$"), auth);
    }


    [Fact]
    public void Auth_SignedHeadersAreAlphabetical()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));

        req.Build();
        var auth = req.Headers["Authorization"];
        var signedHeaders = Regex.Match(auth, @"SignedHeaders=([^,]+),").Groups[1].Value.Split(';');

        var sorted = signedHeaders.OrderBy(h => h, StringComparer.Ordinal).ToArray();
        Assert.Equal(sorted, signedHeaders);
    }


    [Fact]
    public void Auth_CredentialScope_UsesInjectedDate()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));

        req.Build();
        var auth = req.Headers["Authorization"];
        Assert.Contains("/20240524/us-east-1/s3/aws4_request", auth);
    }


    [Fact]
    public void Sign_SameInputs_ProduceIdenticalSignatures()
    {
        using var f = new TempFile();
        var clock = new FakeTimeProvider(FixedClock);

        var auth1 = SignedAuth(f.Path, clock);
        var auth2 = SignedAuth(f.Path, new FakeTimeProvider(FixedClock));

        Assert.Equal(auth1, auth2);
    }


    [Fact]
    public void Sign_DifferentTimes_ProduceDifferentSignatures()
    {
        using var f = new TempFile();
        var auth1 = SignedAuth(f.Path, new FakeTimeProvider(FixedClock));
        var auth2 = SignedAuth(f.Path, new FakeTimeProvider(FixedClock.AddSeconds(1)));

        Assert.NotEqual(auth1, auth2);
    }


    [Fact]
    public void Sign_DifferentSecretKeys_ProduceDifferentSignatures()
    {
        using var f = new TempFile();
        var clock = new FakeTimeProvider(FixedClock);

        var a = new AwsS3UploadRequest(f.Path) { TimeProvider = clock }
            .WithBucket("b", "us-east-1")
            .WithCredentials("AK", "secret-1");
        a.Build();

        var b = new AwsS3UploadRequest(f.Path) { TimeProvider = clock }
            .WithBucket("b", "us-east-1")
            .WithCredentials("AK", "secret-2");
        b.Build();

        Assert.NotEqual(a.Headers["Authorization"], b.Headers["Authorization"]);
    }


    [Fact]
    public void Build_PreservesIdentifier_IfProvided()
    {
        using var f = new TempFile();
        var req = new AwsS3UploadRequest(f.Path)
        {
            Identifier = "fixed-id",
            TimeProvider = new FakeTimeProvider(FixedClock)
        }
            .WithBucket("b", "us-east-1")
            .WithCredentials("ak", "sk");

        var transfer = req.Build();

        Assert.Equal("fixed-id", transfer.Identifier);
    }


    [Fact]
    public void Build_GeneratesIdentifier_IfMissing()
    {
        using var f = new TempFile();
        var req = NewSignedRequest(f.Path, new FakeTimeProvider(FixedClock));
        var transfer = req.Build();

        Assert.True(Guid.TryParse(transfer.Identifier, out _));
    }


    static string SignedAuth(string filePath, TimeProvider clock)
    {
        var req = NewSignedRequest(filePath, clock);
        req.Build();
        return req.Headers["Authorization"];
    }
}
