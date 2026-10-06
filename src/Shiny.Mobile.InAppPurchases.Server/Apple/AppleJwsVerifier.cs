using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server
{
    public enum AppleVerificationFailure
    {
        InvalidFormat,
        InvalidAlgorithm,
        InvalidCertificateChain,
        InvalidSignature,
        InvalidBundleId,
        InvalidAppAppleId,
        InvalidEnvironment
    }


    public sealed class AppleVerificationException(AppleVerificationFailure failure, string message) : Exception(message)
    {
        public AppleVerificationFailure Failure { get; } = failure;
    }
}


namespace Shiny.InAppPurchases.Server.Infrastructure
{
    /// <summary>
    /// Verifies Apple-signed JWS (notifications, transactions, renewal info): ES256 signature by the x5c leaf, and an
    /// x5c chain leaf → Apple intermediate → trusted Apple root carrying Apple's marker OIDs.
    /// </summary>
    sealed class AppleJwsVerifier(
        IReadOnlyList<X509Certificate2> trustedRoots,
        bool onlineRevocationCheck,
        TimeProvider timeProvider
    )
    {
        public const string LeafCertificateOid = "1.2.840.113635.100.6.11.1";
        public const string IntermediateCertificateOid = "1.2.840.113635.100.6.2.1";
        const string RootResourceName = "AppleRootCA-G3.cer";


        public static X509Certificate2 LoadAppleRootCertificate()
        {
            using var stream = typeof(AppleJwsVerifier).Assembly.GetManifestResourceStream(RootResourceName)
                ?? throw new InvalidOperationException($"Embedded resource {RootResourceName} is missing");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return X509CertificateLoader.LoadCertificate(ms.ToArray());
        }


        public T Verify<T>(string jws, JsonTypeInfo<T> typeInfo, Func<T, long?> getSignedDate) where T : class
        {
            if (String.IsNullOrWhiteSpace(jws))
                throw Fail(AppleVerificationFailure.InvalidFormat, "JWS is empty");

            var parts = jws.Split('.');
            if (parts.Length != 3)
                throw Fail(AppleVerificationFailure.InvalidFormat, "JWS must have 3 segments");

            byte[] headerBytes, payloadBytes, signature;
            try
            {
                headerBytes = Base64Url.DecodeFromChars(parts[0]);
                payloadBytes = Base64Url.DecodeFromChars(parts[1]);
                signature = Base64Url.DecodeFromChars(parts[2]);
            }
            catch (FormatException)
            {
                throw Fail(AppleVerificationFailure.InvalidFormat, "JWS segment is not valid base64url");
            }

            string? alg = null;
            var x5c = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(headerBytes);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw Fail(AppleVerificationFailure.InvalidFormat, "JWS header is not a JSON object");

                if (root.TryGetProperty("alg", out var a) && a.ValueKind == JsonValueKind.String)
                    alg = a.GetString();

                if (root.TryGetProperty("x5c", out var chain) && chain.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in chain.EnumerateArray())
                    {
                        if (c.ValueKind == JsonValueKind.String)
                            x5c.Add(c.GetString()!);
                    }
                }
            }
            catch (JsonException)
            {
                throw Fail(AppleVerificationFailure.InvalidFormat, "JWS header is not valid JSON");
            }

            if (alg != "ES256")
                throw Fail(AppleVerificationFailure.InvalidAlgorithm, $"Unsupported JWS algorithm '{alg}' - Apple signs with ES256");

            if (x5c.Count != 3)
                throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS x5c header must contain exactly 3 certificates");

            T? payload;
            try
            {
                payload = JsonSerializer.Deserialize(payloadBytes, typeInfo);
            }
            catch (JsonException ex)
            {
                throw Fail(AppleVerificationFailure.InvalidFormat, "JWS payload is not valid JSON: " + ex.Message);
            }
            if (payload == null)
                throw Fail(AppleVerificationFailure.InvalidFormat, "JWS payload is empty");

            X509Certificate2 leaf, intermediate, rootCert;
            try
            {
                leaf = LoadCertificate(x5c[0]);
                intermediate = LoadCertificate(x5c[1]);
                rootCert = LoadCertificate(x5c[2]);
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS x5c contains an invalid certificate");
            }

            using (leaf)
            using (intermediate)
            using (rootCert)
            {
                if (!trustedRoots.Any(x => x.RawDataMemory.Span.SequenceEqual(rootCert.RawDataMemory.Span)))
                    throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS root certificate is not a trusted Apple root");

                if (!HasExtension(leaf, LeafCertificateOid))
                    throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS signing certificate is missing the Apple App Store receipt signing OID");

                if (!HasExtension(intermediate, IntermediateCertificateOid))
                    throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS intermediate certificate is missing the Apple WWDR OID");

                // Apple's libraries validate at the signed date unless online checks are enabled, so historic
                // transactions keep verifying after an intermediate certificate rotates
                var verificationTime = onlineRevocationCheck
                    ? timeProvider.GetUtcNow()
                    : InAppPurchaseUtils.FromUnixMilliseconds(getSignedDate(payload)) ?? timeProvider.GetUtcNow();

                using var chain = new X509Chain();
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.AddRange(trustedRoots.ToArray());
                chain.ChainPolicy.ExtraStore.Add(intermediate);
                chain.ChainPolicy.RevocationMode = onlineRevocationCheck ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
                chain.ChainPolicy.DisableCertificateDownloads = !onlineRevocationCheck;
                chain.ChainPolicy.VerificationTime = verificationTime.UtcDateTime;

                if (!chain.Build(leaf))
                {
                    var status = String.Join(", ", chain.ChainStatus.Select(x => x.Status));
                    throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS certificate chain is not valid: " + status);
                }

                if (chain.ChainElements.Count != 3 || chain.ChainElements[1].Certificate.Thumbprint != intermediate.Thumbprint)
                    throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS certificate chain did not build through the supplied intermediate");

                using var key = leaf.GetECDsaPublicKey()
                    ?? throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS signing certificate does not contain an EC public key");

                if (key.KeySize != 256)
                    throw Fail(AppleVerificationFailure.InvalidCertificateChain, "JWS signing key is not P-256");

                var signingInput = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                if (!key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                    throw Fail(AppleVerificationFailure.InvalidSignature, "JWS signature is invalid");
            }
            return payload;
        }


        static X509Certificate2 LoadCertificate(string base64Der)
            => X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64Der));


        static bool HasExtension(X509Certificate2 certificate, string oid)
            => certificate.Extensions.Any(x => x.Oid?.Value == oid);


        internal static AppleVerificationException Fail(AppleVerificationFailure failure, string message) => new(failure, message);
    }
}
