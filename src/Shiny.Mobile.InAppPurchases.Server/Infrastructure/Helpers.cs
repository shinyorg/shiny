using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.InAppPurchases.Server.Infrastructure;


[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(AppleNotificationPayload))]
[JsonSerializable(typeof(AppleTransaction))]
[JsonSerializable(typeof(AppleRenewalInfo))]
[JsonSerializable(typeof(AppleSignedPayloadBody))]
[JsonSerializable(typeof(AppleTransactionInfoResponse))]
[JsonSerializable(typeof(AppleStatusResponse))]
[JsonSerializable(typeof(AppleTestNotificationResponse))]
[JsonSerializable(typeof(AppleApiError))]
[JsonSerializable(typeof(PubSubPushEnvelope))]
[JsonSerializable(typeof(GoogleDeveloperNotification))]
[JsonSerializable(typeof(GoogleSubscriptionPurchase))]
[JsonSerializable(typeof(GoogleProductPurchase))]
[JsonSerializable(typeof(GoogleServiceAccountCredentials))]
[JsonSerializable(typeof(GoogleTokenResponse))]
[JsonSerializable(typeof(GoogleApiErrorResponse))]
[JsonSerializable(typeof(PurchaseVerificationRequest))]
[JsonSerializable(typeof(VerifiedPurchase))]
partial class InAppPurchaseServerJsonContext : JsonSerializerContext;


static class InAppPurchaseUtils
{
    public static DateTimeOffset? FromUnixMilliseconds(long? milliseconds)
        => milliseconds is long ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;


    public static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var guid) ? guid : null;


    public static StoreEnvironment ParseAppleEnvironment(string? value) => value switch
    {
        "Production" => StoreEnvironment.Production,
        "Sandbox" => StoreEnvironment.Sandbox,
        "Xcode" => StoreEnvironment.Xcode,
        "LocalTesting" => StoreEnvironment.LocalTesting,
        _ => StoreEnvironment.Unknown
    };
}


static class JwtHelper
{
    public static string CreateEs256(ECDsa key, string keyId, Action<Utf8JsonWriter> writeClaims)
    {
        var input = Encode(w =>
        {
            w.WriteString("alg", "ES256");
            w.WriteString("kid", keyId);
            w.WriteString("typ", "JWT");
        }) + "." + Encode(writeClaims);

        var signature = key.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return input + "." + Base64Url.EncodeToString(signature);
    }


    public static string CreateRs256(RSA key, string? keyId, Action<Utf8JsonWriter> writeClaims)
    {
        var input = Encode(w =>
        {
            w.WriteString("alg", "RS256");
            if (!String.IsNullOrWhiteSpace(keyId))
                w.WriteString("kid", keyId);
            w.WriteString("typ", "JWT");
        }) + "." + Encode(writeClaims);

        var signature = key.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return input + "." + Base64Url.EncodeToString(signature);
    }


    static string Encode(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
        }
        return Base64Url.EncodeToString(buffer.WrittenSpan);
    }
}


/// <summary>
/// RFC 3339 timestamps from Google APIs can carry nanosecond precision, which DateTimeOffset cannot parse directly.
/// </summary>
sealed class Rfc3339DateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var value = reader.GetString();
        if (String.IsNullOrEmpty(value))
            return null;

        var dot = value.IndexOf('.');
        if (dot > 0)
        {
            var end = dot + 1;
            while (end < value.Length && Char.IsAsciiDigit(value[end]))
                end++;

            if (end - dot - 1 > 7)
                value = value[..(dot + 8)] + value[end..];
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
            return result;

        throw new JsonException($"Invalid RFC 3339 timestamp '{value}'");
    }


    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString("O", CultureInfo.InvariantCulture));
    }
}
