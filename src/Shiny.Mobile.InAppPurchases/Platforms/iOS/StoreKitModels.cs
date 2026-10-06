using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.InAppPurchases;


sealed class NativeError
{
    public string Code { get; set; } = "Unknown";
    public string? Message { get; set; }
    public string? Native { get; set; }
}


sealed class NativeProduct
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string DisplayPrice { get; set; } = "";
    public string Price { get; set; } = "0";
    public string CurrencyCode { get; set; } = "";
    public string Type { get; set; } = "";
    public string? SubscriptionGroupId { get; set; }
    public string? Period { get; set; }
    public List<NativeOffer>? Offers { get; set; }
}


sealed class NativeOffer
{
    public string? Id { get; set; }
    public string Type { get; set; } = "";
    public string DisplayPrice { get; set; } = "";
    public string Price { get; set; } = "0";
    public string Period { get; set; } = "";
    public int PeriodCount { get; set; }
    public string PaymentMode { get; set; } = "";
}


sealed class NativeTransaction
{
    public string Id { get; set; } = null!;
    public string OriginalId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public string ProductType { get; set; } = "";
    public double PurchaseDate { get; set; }
    public double? ExpirationDate { get; set; }
    public double? RevocationDate { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsUpgraded { get; set; }
    public bool IsFinished { get; set; }
    public bool IsAutoRenewing { get; set; }
    public string Jws { get; set; } = "";
    public string? Json { get; set; }
    public string BundleId { get; set; } = "";
    public string? Environment { get; set; }
    public string? AppAccountToken { get; set; }
}


sealed class NativePurchaseResult
{
    public string Status { get; set; } = "";
    public NativeTransaction? Transaction { get; set; }
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NativeError))]
[JsonSerializable(typeof(List<NativeProduct>))]
[JsonSerializable(typeof(List<NativeTransaction>))]
[JsonSerializable(typeof(NativeTransaction))]
[JsonSerializable(typeof(NativePurchaseResult))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(NativePurchaseOptions))]
partial class StoreKitJsonContext : JsonSerializerContext;


sealed class NativePurchaseOptions
{
    public string? AppAccountToken { get; set; }
    public int Quantity { get; set; } = 1;
}


static class StoreKitMapper
{
    public static InAppPurchaseException ToException(string errorJson)
    {
        NativeError? error = null;
        try
        {
            error = JsonSerializer.Deserialize(errorJson, StoreKitJsonContext.Default.NativeError);
        }
        catch (JsonException)
        {
        }
        error ??= new NativeError { Message = errorJson };

        var code = Enum.TryParse<InAppPurchaseErrorCode>(error.Code, out var parsed) ? parsed : InAppPurchaseErrorCode.Unknown;
        return new InAppPurchaseException(code, error.Message ?? error.Code)
        {
            // "Cancelled" is not a InAppPurchaseErrorCode - PurchaseAsync turns it into PurchaseResultStatus.Cancelled
            NativeErrorCode = error.Native ?? error.Code
        };
    }


    public static bool IsCancellation(InAppPurchaseException ex) => ex.NativeErrorCode?.Contains("userCancelled") == true;


    public static StoreProduct ToProduct(NativeProduct native)
    {
        var price = ParseDecimal(native.Price);
        var offers = new List<SubscriptionOffer>();

        if (native.Type == "autoRenewable" && native.Offers != null)
        {
            var recurring = new PricingPhase(
                native.DisplayPrice,
                price,
                native.CurrencyCode,
                native.Period ?? "",
                0,
                SubscriptionPaymentMode.Recurring
            );

            foreach (var offer in native.Offers)
            {
                var phase = new PricingPhase(
                    offer.DisplayPrice,
                    ParseDecimal(offer.Price),
                    native.CurrencyCode,
                    offer.Period,
                    offer.PeriodCount,
                    offer.PaymentMode switch
                    {
                        "freeTrial" => SubscriptionPaymentMode.FreeTrial,
                        "payUpFront" => SubscriptionPaymentMode.PayUpFront,
                        _ => SubscriptionPaymentMode.PayAsYouGo
                    }
                );
                offers.Add(new SubscriptionOffer(
                    offer.Id,
                    null,
                    null,
                    offer.Type == "introductory",
                    [phase, recurring],
                    []
                ));
            }
        }

        return new StoreProduct(
            native.Id,
            native.Type == "autoRenewable" ? ProductType.Subscription : ProductType.OneTime,
            native.DisplayName,
            native.Description,
            native.DisplayPrice,
            price,
            native.CurrencyCode,
            offers,
            native.SubscriptionGroupId
        );
    }


    public static Purchase ToPurchase(NativeTransaction native) => new(
        StorePlatform.AppStore,
        native.ProductId,
        native.Id,
        native.OriginalId,
        native.RevocationDate == null ? PurchaseState.Purchased : PurchaseState.Revoked,
        FromMillis(native.PurchaseDate),
        native.ExpirationDate is { } exp ? FromMillis(exp) : null,
        native.IsFinished,
        native.IsAutoRenewing,
        native.Quantity,
        Guid.TryParse(native.AppAccountToken, out var token) ? token : null,
        native.Jws,
        native.BundleId,
        native.Environment switch
        {
            "Production" => StoreEnvironment.Production,
            "Sandbox" => StoreEnvironment.Sandbox,
            "Xcode" => StoreEnvironment.Xcode,
            _ => StoreEnvironment.Unknown
        },
        native.Json,
        null,
        native.RevocationDate is { } rev ? FromMillis(rev) : null
    );


    static DateTimeOffset FromMillis(double ms) => DateTimeOffset.FromUnixTimeMilliseconds((long)ms);

    static decimal ParseDecimal(string value)
        => Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}
