using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Shiny.DeviceTests.Tests;


// mirrors of the runner's HttpTransferFixture shapes (tests/Shiny.DeviceTests.Runner/Fixtures)
public record RecordedRequest(string Method, string? Range, Dictionary<string, string> Headers);

public record UploadRecord(
    string Id,
    string Method,
    string ContentType,
    long Length,
    string Sha256,
    string? FileName,
    string? FileFieldName,
    Dictionary<string, string> Form,
    Dictionary<string, string> Headers,
    int Count
);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<RecordedRequest>))]
[JsonSerializable(typeof(UploadRecord))]
public partial class HttpFixtureJsonContext : JsonSerializerContext;


// byte i of every runner download = (i * 31 + 7) mod 251 - see FilePattern in the runner
public static class FilePattern
{
    public static byte[] Create(long length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)((((long)i) * 31 + 7) % 251);
        return bytes;
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static async Task<string> Sha256Async(string path)
    {
        await using var fs = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(fs));
    }
}
