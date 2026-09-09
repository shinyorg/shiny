using Shiny.Beacons;

namespace Sample.Shared.Maui.Pages.Beacons;


[ShellMap<BeaconBroadcastPage>("beaconbroadcast")]
public partial class BeaconBroadcastViewModel(IBeaconBroadcaster broadcaster) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string uuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";
    [ObservableProperty] string major = "1";
    [ObservableProperty] string minor = "1";
    [ObservableProperty] string eddystoneNamespace = "0102030405060708090A";
    [ObservableProperty] string eddystoneInstance = "0B0C0D0E0F10";
    [ObservableProperty] string url = "https://shinylib.net/";
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] string isBroadcastingText = "No";


    public void OnAppearing() => this.RefreshState();
    public void OnDisappearing() { }


    [RelayCommand]
    Task BroadcastIBeacon() => this.Run(async () =>
    {
        if (!Guid.TryParse(this.Uuid, out var uuid))
            return "Enter a valid UUID";

        var major = UInt16.TryParse(this.Major, out var m) ? m : (ushort)0;
        var minor = UInt16.TryParse(this.Minor, out var n) ? n : (ushort)0;

        await broadcaster.StartIBeacon(uuid, major, minor);
        return $"Broadcasting iBeacon {major}/{minor}";
    });


    [RelayCommand]
    Task BroadcastUid() => this.Run(async () =>
    {
        var uid = EddystoneUid.Parse(this.EddystoneNamespace.Trim(), this.EddystoneInstance.Trim());
        await broadcaster.StartEddystoneUid(uid);
        return $"Broadcasting Eddystone-UID {uid}";
    });


    [RelayCommand]
    Task BroadcastUrl() => this.Run(async () =>
    {
        await broadcaster.StartEddystoneUrl(this.Url.Trim());
        return $"Broadcasting Eddystone-URL {this.Url}";
    });


    [RelayCommand]
    void Stop()
    {
        broadcaster.Stop();
        this.Status = "Stopped";
        this.RefreshState();
    }


    async Task Run(Func<Task<string>> action)
    {
        try
        {
            this.Status = await action();
        }
        catch (Exception ex)
        {
            // PlatformNotSupportedException is the expected answer for Eddystone on Apple and for
            // any broadcast on Linux - show it rather than swallowing it
            this.Status = ex.Message;
        }
        this.RefreshState();
    }


    void RefreshState() => this.IsBroadcastingText = broadcaster.IsBroadcasting ? "Yes" : "No";
}
