namespace Sample.Shared.Maui.BleHosting;


/// <summary>
/// UUIDs for the source-generated hosting sample. Deliberately a different service UUID from the
/// hand-written <see cref="Pages.BLE.BleHostingViewModel"/> sample so both pages can run at once -
/// BleHostingManager keys its services by UUID and refuses a second registration of the same one.
/// </summary>
static class SampleGattUuids
{
    public const string Service = "A495FF40-C5B1-4B44-B512-1370F02D74DE";
    public const string Greeting = "A495FF41-C5B1-4B44-B512-1370F02D74DE";
    public const string Command = "A495FF42-C5B1-4B44-B512-1370F02D74DE";
    public const string Ticker = "A495FF43-C5B1-4B44-B512-1370F02D74DE";
    public const string Echo = "A495FF44-C5B1-4B44-B512-1370F02D74DE";
    public const string Psm = "A495FF45-C5B1-4B44-B512-1370F02D74DE";
    public const string Model = "A495FF46-C5B1-4B44-B512-1370F02D74DE";
}
