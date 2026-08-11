namespace Sample.Shared.Maui.BleHosting;


/// <summary>
/// Shared counters the generated services write into and the page binds to. Keeping it out of the
/// view model lets the service classes stay plain and DI-injectable.
/// </summary>
public partial class SampleBleHostingActivity : ObservableObject
{
    [ObservableProperty] string status = "Idle";
    [ObservableProperty] int reads;
    [ObservableProperty] int writes;
    [ObservableProperty] int subscribers;
    [ObservableProperty] string lastWrite = "(none)";
    [ObservableProperty] string lastCentral = "(none)";


    /// <summary>
    /// Applies an update on the UI thread. GATT callbacks arrive on platform threads.
    /// </summary>
    public void Update(Action apply) => MainThread.BeginInvokeOnMainThread(apply);
}
