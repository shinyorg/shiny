using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Shiny.InAppPurchases;

namespace Sample.InAppPurchases.Maui;


/// <summary>
/// Calls Sample.InAppPurchases.Server's <c>POST /iap/verify</c> (Shiny.InAppPurchases.Server <c>MapInAppPurchaseVerification</c>).
/// </summary>
public class PurchaseApi(HttpClient http, ILogger<PurchaseApi> logger)
{
    public static bool IsTestMode => SampleConfig.ServerUrl == null;


    public async Task<VerifyResponse> VerifyAsync(Purchase purchase, CancellationToken cancelToken = default)
    {
        if (IsTestMode)
        {
            logger.LogWarning("LOCAL TEST MODE - trusting {TransactionId} without server verification", purchase.TransactionId);
            return new VerifyResponse
            {
                IsValid = true,
                IsActive = purchase.State == PurchaseState.Purchased
            };
        }

        var request = new VerifyRequest(purchase.Platform.ToString(), purchase.VerificationData, purchase.ProductId);
        var uri = new Uri(new Uri(SampleConfig.ServerUrl!), "iap/verify");

        // In a real app this request carries your user's auth token - the server must grant to the
        // authenticated user, never to an account id sent in the body
        using var response = await http
            .PostAsJsonAsync(uri, request, SampleJsonContext.Default.VerifyRequest, cancelToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync(SampleJsonContext.Default.VerifyResponse, cancelToken)
            .ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Empty verification response");
    }
}


public record VerifyRequest(string Platform, string VerificationData, string ProductId);


public record VerifyResponse
{
    public bool IsValid { get; init; }
    public bool IsActive { get; init; }
    public string? Error { get; init; }
    public string? Environment { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}


[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(VerifyRequest))]
[JsonSerializable(typeof(VerifyResponse))]
partial class SampleJsonContext : JsonSerializerContext;
