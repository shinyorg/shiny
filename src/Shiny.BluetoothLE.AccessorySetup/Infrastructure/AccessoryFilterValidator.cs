namespace Shiny.BluetoothLE.AccessorySetup.Infrastructure;


/// <summary>
/// Checks picker requests up front, so a bad filter fails with a message naming the problem instead of the platform's
/// generic "invalid request" (iOS) or a picker that silently never finds anything (Android).
/// </summary>
static class AccessoryFilterValidator
{
    public static void Validate(IReadOnlyList<AccessoryPickerItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
            throw Invalid("At least one AccessoryPickerItem is required");

        foreach (var item in items)
        {
            if (item is null)
                throw Invalid("A picker item is null");

            if (String.IsNullOrWhiteSpace(item.Name))
                throw Invalid("Every AccessoryPickerItem needs a Name");

            Validate(item.Filter, item.Name);
        }
    }


    public static void Validate(AccessoryDiscoveryFilter filter, string itemName)
    {
        if (filter is null)
            throw Invalid($"'{itemName}' has no Filter");

        if (filter.ServiceUuid == null && filter.CompanyIdentifier == null)
            throw Invalid($"'{itemName}' needs a ServiceUuid or a CompanyIdentifier - the system will not search on a name alone");

        if (filter.ServiceUuid != null && !IsUuid(filter.ServiceUuid))
            throw Invalid($"'{itemName}' ServiceUuid '{filter.ServiceUuid}' is not a 16-bit, 32-bit or 128-bit UUID");

        if (filter.ServiceData != null && filter.ServiceUuid == null)
            throw Invalid($"'{itemName}' sets ServiceData without the ServiceUuid it is advertised under");

        if (filter.ServiceDataMask != null && (filter.ServiceData == null || filter.ServiceDataMask.Length != filter.ServiceData.Length))
            throw Invalid($"'{itemName}' ServiceDataMask must be the same length as ServiceData");

        if (filter.ManufacturerData != null && filter.CompanyIdentifier == null)
            throw Invalid($"'{itemName}' sets ManufacturerData without a CompanyIdentifier");

        if (filter.ManufacturerDataMask != null && (filter.ManufacturerData == null || filter.ManufacturerDataMask.Length != filter.ManufacturerData.Length))
            throw Invalid($"'{itemName}' ManufacturerDataMask must be the same length as ManufacturerData");

        if (filter.NameSubstring != null && filter.NameSubstring.Length == 0)
            throw Invalid($"'{itemName}' NameSubstring is empty - leave it null instead");
    }


    /// <summary>
    /// The full 128-bit form of a 16-bit, 32-bit or 128-bit UUID string, expanded on the Bluetooth base UUID - what
    /// Android's <c>ParcelUuid</c> needs.
    /// </summary>
    public static Guid ToFullUuid(string uuid)
    {
        var value = uuid.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            value = value[2..];

        if (value.Length is 4 or 8 && UInt32.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var shortUuid))
            return Guid.Parse($"{shortUuid:x8}-0000-1000-8000-00805f9b34fb");

        return Guid.Parse(value);
    }


    static bool IsUuid(string uuid)
    {
        try
        {
            ToFullUuid(uuid);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }


    static AccessorySetupException Invalid(string message) => new(AccessorySetupErrorCode.InvalidRequest, message);
}
