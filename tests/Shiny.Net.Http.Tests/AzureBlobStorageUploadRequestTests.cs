using System;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class AzureBlobStorageUploadRequestTests
{
    static readonly DateTimeOffset FixedClock = new(2024, 5, 24, 12, 34, 56, TimeSpan.Zero);


    // -------- Fluent builders --------

    [Fact]
    public void WithBlobContainer_BuildsCanonicalUri()
    {
        var req = new AzureBlobStorageUploadRequest("/tmp/x.bin")
            .WithBlobContainer("mytenant", "mycontainer");

        Assert.Equal("https://mytenant.blob.core.windows.net/mycontainer", req.Uri);
    }


    [Fact]
    public void WithCustomUri_OverridesContainerComposition()
    {
        var req = new AzureBlobStorageUploadRequest("/tmp/x.bin")
            .WithBlobContainer("t", "c")
            .WithCustomUri("https://example.com/blob");

        Assert.Equal("https://example.com/blob", req.Uri);
    }


    [Fact]
    public void Fluent_AllSetters_PopulateProperties()
    {
        var when = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var req = new AzureBlobStorageUploadRequest("/tmp/x.bin")
            .WithMeteredConnection()
            .WithHeader("X-A", "v")
            .WithRemoteFileName("renamed.bin")
            .WithSasToken("sv=2024-08&sig=abc")
            .WithSharedKeyAuthorization("the-key", Version: when, AuthDate: when);

        Assert.True(req.UseMeteredConnection);
        Assert.Equal("v", req.Headers["X-A"]);
        Assert.Equal("renamed.bin", req.RemoteFileName);
        Assert.Equal("sv=2024-08&sig=abc", req.SasToken);
        Assert.Equal("the-key", req.SharedAuthorizationKey);
        Assert.Equal(when, req.AuthVersion);
        Assert.Equal(when, req.AuthDate);
    }


    // -------- Build() validation --------

    [Fact]
    public void Build_NoUri_Throws()
    {
        using var f = new TempFile();
        var req = new AzureBlobStorageUploadRequest(f.Path);
        Assert.Throws<InvalidOperationException>(() => req.Build());
    }


    [Fact]
    public void Build_InvalidUri_Throws()
    {
        using var f = new TempFile();
        var req = new AzureBlobStorageUploadRequest(f.Path).WithCustomUri("not-a-uri");
        Assert.Throws<InvalidOperationException>(() => req.Build());
    }


    // -------- Generated request shape --------

    [Fact]
    public void Build_AppendsRemoteFileName_ToUri()
    {
        using var f = new TempFile();
        var transfer = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .WithRemoteFileName("upload.bin")
            .Build();

        Assert.Equal("https://t.blob.core.windows.net/c/upload.bin", transfer.Uri);
        Assert.Equal("PUT", transfer.HttpMethod);
        Assert.Equal(TransferType.UploadRaw, transfer.Type);
    }


    [Fact]
    public void Build_DefaultsBlobName_ToLocalFileName()
    {
        using var f = new TempFile();
        var fileName = System.IO.Path.GetFileName(f.Path);
        var transfer = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .Build();

        Assert.EndsWith("/" + fileName, transfer.Uri);
    }


    [Fact]
    public void Build_SetsRequiredHeaders()
    {
        using var f = new TempFile(bytes: 999);
        var req = new AzureBlobStorageUploadRequest(f.Path).WithBlobContainer("t", "c");
        req.Build();

        Assert.Equal("999", req.Headers["Content-Length"]);
        Assert.Equal("BlockBlob", req.Headers["x-ms-blob-type"]);
        Assert.Contains("Content-Disposition", req.Headers.Keys);
    }


    [Fact]
    public void Build_ContentDisposition_UsesRemoteFileName()
    {
        using var f = new TempFile();
        var req = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .WithRemoteFileName("custom.dat");

        req.Build();

        Assert.Equal("attachment; filename=\"custom.dat\"", req.Headers["Content-Disposition"]);
    }


    // -------- SAS token path --------

    [Fact]
    public void Build_WithSasToken_AppendsToUri()
    {
        using var f = new TempFile();
        var transfer = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .WithSasToken("sv=2024-08-04&sig=abc%2F123")
            .Build();

        Assert.Contains("?sv=2024-08-04&sig=abc%2F123", transfer.Uri);
    }


    [Fact]
    public void Build_WithSasToken_DoesNotAddAuthHeader()
    {
        using var f = new TempFile();
        var req = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .WithSasToken("sv=2024-08-04&sig=abc");

        req.Build();

        Assert.False(req.Headers.ContainsKey("Authorization"));
        Assert.False(req.Headers.ContainsKey("x-ms-date"));
    }


    // -------- Shared Key path --------

    [Fact]
    public void Build_WithSharedKey_AddsAuthAndDateHeaders()
    {
        using var f = new TempFile();
        var req = new AzureBlobStorageUploadRequest(f.Path) { TimeProvider = new FakeTimeProvider(FixedClock) }
            .WithBlobContainer("t", "c")
            .WithSharedKeyAuthorization("my-shared-key");

        req.Build();

        Assert.Equal("SharedKey my-shared-key", req.Headers["Authorization"]);
        Assert.Equal("2024-05-24", req.Headers["x-ms-date"]);
        Assert.Equal("2024-05-24", req.Headers["x-ms-version"]);
    }


    [Fact]
    public void Build_WithSharedKey_HonorsExplicitAuthDate()
    {
        using var f = new TempFile();
        var when = new DateTimeOffset(2023, 11, 7, 0, 0, 0, TimeSpan.Zero);

        var req = new AzureBlobStorageUploadRequest(f.Path) { TimeProvider = new FakeTimeProvider(FixedClock) }
            .WithBlobContainer("t", "c")
            .WithSharedKeyAuthorization("key", AuthDate: when);

        req.Build();

        Assert.Equal("2023-11-07", req.Headers["x-ms-date"]);
    }


    [Fact]
    public void Build_PreservesIdentifier_IfProvided()
    {
        using var f = new TempFile();
        var req = new AzureBlobStorageUploadRequest(f.Path)
        {
            Identifier = "fixed",
        }.WithBlobContainer("t", "c");
        var transfer = req.Build();

        Assert.Equal("fixed", transfer.Identifier);
    }


    [Fact]
    public void Build_GeneratesIdentifier_IfMissing()
    {
        using var f = new TempFile();
        var transfer = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .Build();

        Assert.True(Guid.TryParse(transfer.Identifier, out _));
    }


    [Fact]
    public void UseMeteredConnection_PropagatesToTransferRequest()
    {
        using var f = new TempFile();
        var transfer = new AzureBlobStorageUploadRequest(f.Path)
            .WithBlobContainer("t", "c")
            .WithMeteredConnection()
            .Build();

        Assert.True(transfer.UseMeteredConnection);
    }
}
