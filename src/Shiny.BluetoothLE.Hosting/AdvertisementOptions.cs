namespace Shiny.BluetoothLE.Hosting;


public record AdvertisementOptions(
    /// <summary>
    /// Set the local name of the advertisement
    /// </summary>
    string? LocalName = null,

    /// <summary>
    /// GATT services to advertise
    /// </summary>
    params string[] ServiceUuids
);