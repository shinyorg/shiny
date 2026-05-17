using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Shiny.Net.Http;


// https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutObject.html
/// <summary>
/// Fluent builder that produces a signed <see cref="HttpTransferRequest"/> for uploading
/// a local file to AWS S3 using PutObject (presigned URL or IAM credentials).
/// </summary>
public class AwsS3UploadRequest(string localFilePath)
{
    /// <summary>Gets the path of the local file to upload.</summary>
    public string LocalFilePath => localFilePath;

    /// <summary>Optional transfer identifier. A GUID is generated if not provided.</summary>
    public string? Identifier { get; set; }

    /// <summary>Allow the transfer to run on a metered (e.g. cellular) connection.</summary>
    public bool UseMeteredConnection { get; set; }

    /// <summary>Additional HTTP headers attached to the upload request.</summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>The destination S3 bucket name.</summary>
    public string? BucketName { get; set; }

    /// <summary>The AWS region for the destination bucket.</summary>
    public string? Region { get; set; }

    /// <summary>Object key within the bucket. Defaults to the local file name.</summary>
    public string? ObjectKey { get; set; }

    /// <summary>Optional custom endpoint URI (for S3-compatible services).</summary>
    public string? CustomUri { get; set; }

    /// <summary>When set, the upload uses this presigned PUT URL instead of credentials.</summary>
    public string? PresignedUrl { get; set; }

    /// <summary>IAM access key id used to sign the request.</summary>
    public string? AccessKeyId { get; set; }

    /// <summary>IAM secret access key used to sign the request.</summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>Optional STS session token for temporary credentials.</summary>
    public string? SessionToken { get; set; }

    /// <summary>HTTP Content-Type header value. Defaults to application/octet-stream.</summary>
    public string? ContentType { get; set; }

    /// <summary>Optional S3 storage class (e.g. STANDARD_IA, GLACIER).</summary>
    public string? StorageClass { get; set; }


    /// <summary>Sets the destination bucket and region.</summary>
    public AwsS3UploadRequest WithBucket(string bucketName, string region)
    {
        this.BucketName = bucketName;
        this.Region = region;
        return this;
    }


    /// <summary>Sets the object key (path within the bucket).</summary>
    public AwsS3UploadRequest WithObjectKey(string objectKey)
    {
        this.ObjectKey = objectKey;
        return this;
    }


    /// <summary>Uses a custom endpoint URI in place of the standard amazonaws.com host.</summary>
    public AwsS3UploadRequest WithCustomUri(string uri)
    {
        this.CustomUri = uri;
        return this;
    }


    /// <summary>Authenticates the upload using a presigned PUT URL.</summary>
    public AwsS3UploadRequest WithPresignedUrl(string presignedUrl)
    {
        this.PresignedUrl = presignedUrl;
        return this;
    }


    /// <summary>Authenticates the upload using IAM credentials and AWS Signature V4.</summary>
    public AwsS3UploadRequest WithCredentials(string accessKeyId, string secretAccessKey, string? sessionToken = null)
    {
        this.AccessKeyId = accessKeyId;
        this.SecretAccessKey = secretAccessKey;
        this.SessionToken = sessionToken;
        return this;
    }


    /// <summary>Allows the transfer to run over a metered connection.</summary>
    public AwsS3UploadRequest WithMeteredConnection()
    {
        this.UseMeteredConnection = true;
        return this;
    }


    /// <summary>Adds a custom HTTP header to the upload request.</summary>
    public AwsS3UploadRequest WithHeader(string key, string value)
    {
        this.Headers.Add(key, value);
        return this;
    }


    /// <summary>Sets the Content-Type header for the upload.</summary>
    public AwsS3UploadRequest WithContentType(string contentType)
    {
        this.ContentType = contentType;
        return this;
    }


    /// <summary>Sets the S3 storage class header for the upload.</summary>
    public AwsS3UploadRequest WithStorageClass(string storageClass)
    {
        this.StorageClass = storageClass;
        return this;
    }


    /// <summary>
    /// Builds an <see cref="HttpTransferRequest"/> that uploads the local file to S3.
    /// Either a presigned URL or IAM credentials must be configured first.
    /// </summary>
    public HttpTransferRequest Build()
    {
        this.Identifier ??= Guid.NewGuid().ToString();
        var fileInfo = new FileInfo(this.LocalFilePath);

        if (!fileInfo.Exists)
            throw new FileNotFoundException("Local file not found", this.LocalFilePath);

        string uri;

        if (this.PresignedUrl != null)
        {
            uri = this.PresignedUrl;
            this.Headers.TryAdd("Content-Length", fileInfo.Length.ToString());
        }
        else if (this.AccessKeyId != null && this.SecretAccessKey != null)
        {
            if (String.IsNullOrWhiteSpace(this.BucketName))
                throw new InvalidOperationException("BucketName is required - use WithBucket()");
            if (String.IsNullOrWhiteSpace(this.Region))
                throw new InvalidOperationException("Region is required - use WithBucket()");

            var objectKey = this.ObjectKey ?? Path.GetFileName(this.LocalFilePath);
            uri = this.CustomUri ?? $"https://{this.BucketName}.s3.{this.Region}.amazonaws.com/{Uri.EscapeDataString(objectKey)}";

            this.SignRequest(uri, fileInfo, objectKey);
        }
        else
        {
            throw new InvalidOperationException("Authentication is required - use WithPresignedUrl() or WithCredentials()");
        }

        return new HttpTransferRequest(
            this.Identifier,
            uri,
            TransferType.UploadRaw,
            this.LocalFilePath,
            this.UseMeteredConnection,
            null,
            this.Headers
        )
        {
            HttpMethod = "PUT"
        };
    }


    void SignRequest(string uri, FileInfo fileInfo, string objectKey)
    {
        var now = DateTimeOffset.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var host = new Uri(uri).Host;
        var contentType = this.ContentType ?? "application/octet-stream";

        // S3 allows UNSIGNED-PAYLOAD so we don't need to hash potentially large files
        var payloadHash = "UNSIGNED-PAYLOAD";

        this.Headers["Host"] = host;
        this.Headers["x-amz-date"] = amzDate;
        this.Headers["x-amz-content-sha256"] = payloadHash;
        this.Headers["Content-Length"] = fileInfo.Length.ToString();
        this.Headers["Content-Type"] = contentType;

        if (this.SessionToken != null)
            this.Headers["x-amz-security-token"] = this.SessionToken;

        if (this.StorageClass != null)
            this.Headers["x-amz-storage-class"] = this.StorageClass;

        // Step 1: Canonical Request
        var signedHeaderKeys = this.Headers.Keys
            .Select(k => k.ToLowerInvariant())
            .OrderBy(k => k)
            .ToList();

        var signedHeaders = String.Join(";", signedHeaderKeys);

        var canonicalHeaders = String.Join(
            "",
            signedHeaderKeys.Select(k =>
            {
                var value = this.Headers.First(h => h.Key.Equals(k, StringComparison.OrdinalIgnoreCase)).Value;
                return $"{k}:{value.Trim()}\n";
            })
        );

        var encodedKey = "/" + String.Join("/", objectKey.Split('/').Select(Uri.EscapeDataString));
        var canonicalRequest = String.Join(
            "\n",
            "PUT",
            encodedKey,
            "",  // no query string
            canonicalHeaders,
            signedHeaders,
            payloadHash
        );

        // Step 2: String to Sign
        var credentialScope = $"{dateStamp}/{this.Region}/s3/aws4_request";
        var stringToSign = String.Join(
            "\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))
        );

        // Step 3: Signing Key
        var signingKey = GetSignatureKey(this.SecretAccessKey!, dateStamp, this.Region!, "s3");

        // Step 4: Signature
        var signature = Hex(HmacSha256(signingKey, stringToSign));

        // Step 5: Authorization Header
        this.Headers["Authorization"] = $"AWS4-HMAC-SHA256 Credential={this.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }


    static byte[] GetSignatureKey(string key, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        return HmacSha256(kService, "aws4_request");
    }


    static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }


    static string Hex(byte[] data)
        => BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
}
