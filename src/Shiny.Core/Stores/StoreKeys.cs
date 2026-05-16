namespace Shiny.Stores;


/// <summary>
/// Well-known DI service keys for IKeyValueStore registrations
/// </summary>
public static class StoreKeys
{
    /// <summary>The default settings store (platform-native: SharedPreferences, NSUserDefaults, etc.)</summary>
    public const string Default = "settings";

    /// <summary>The secure/encrypted store (Keychain, AndroidKeyStore, DPAPI, etc.)</summary>
    public const string Secure = "secure";
}
