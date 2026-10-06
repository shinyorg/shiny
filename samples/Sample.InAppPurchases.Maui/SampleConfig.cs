namespace Sample.InAppPurchases.Maui;


/// <summary>
/// Everything store-specific lives here - see readme.md for how each value maps to App Store Connect / Play Console.
/// </summary>
public static class SampleConfig
{
    /// <summary>
    /// Base URL of samples/Sample.InAppPurchases.Server (e.g. your dev tunnel: "https://abc123-5001.usw2.devtunnels.ms/").
    /// null = LOCAL TEST MODE: purchases are trusted on the device without server verification.
    /// That is fine for poking at the purchase sheet, but NEVER ship an app that grants without verifying.
    /// </summary>
    public const string? ServerUrl = null;

    /// <summary>Consumable - can be bought again once finished with consume: true</summary>
    public const string Coins = "coins_100";

    /// <summary>Non-consumable - owned forever, restorable</summary>
    public const string RemoveAds = "remove_ads";

    /// <summary>Auto-renewable subscription (Apple: in a subscription group, Google: with a monthly base plan)</summary>
    public const string Premium = "premium_monthly";

    public const int CoinsPerPack = 100;

    public static readonly string[] ProductIds = [Coins, RemoveAds, Premium];

    /// <summary>
    /// Google Play has no concept of consumables - you decide when finishing. Keep this list in sync with your catalog.
    /// </summary>
    public static bool IsConsumable(string productId) => productId == Coins;
}
