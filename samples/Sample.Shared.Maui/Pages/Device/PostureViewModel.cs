using Shiny.Infrastructure;


namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<PosturePage>("posture")]
public partial class PostureViewModel(IScreenPosture posture, IMainThread mainThread) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] ScreenPosture current = ScreenPosture.None;
    [ObservableProperty] string supported = "";
    [ObservableProperty] string state = "";
    [ObservableProperty] string orientation = "";
    [ObservableProperty] string fold = "";
    [ObservableProperty] string hinge = "";
    [ObservableProperty] string segments = "";
    [ObservableProperty] string occlusions = "";
    [ObservableProperty] string supportedPostures = "";

    public void OnAppearing()
    {
        posture.Changed += this.OnChanged;
        this.Show(posture.Current);
    }

    public void OnDisappearing() => posture.Changed -= this.OnChanged;

    void OnChanged(object? sender, ScreenPosture value)
        => mainThread.BeginInvokeOnMainThread(() => this.Show(value));

    void Show(ScreenPosture value)
    {
        this.Supported = posture.IsSupported ? "Posture is supported on this platform" : "Posture is not supported on this platform";
        this.State = value.IsTabletop ? $"{value.State} (tabletop)" : value.IsBook ? $"{value.State} (book)" : value.State.ToString();
        this.Orientation = value.Orientation.ToString();
        this.Fold = value.Fold?.ToString() ?? "none";
        this.Hinge = value.HingeAngleDegrees is { } angle ? $"{value.Hinge} at {angle:0}°" : value.Hinge.ToString();
        this.Occlusions = value.Occlusions.Count == 0 ? "none" : string.Join(", ", value.Occlusions);
        this.SupportedPostures = posture.SupportedPostures.Count == 0 ? "unknown" : string.Join(", ", posture.SupportedPostures);

        var window = Application.Current?.Windows.FirstOrDefault();
        var split = window == null ? null : value.Segments(new ScreenSize(window.Width, window.Height));
        this.Segments = split is { } s ? $"{s.First} | {s.Second}" : "none";

        // last: the page redraws the map when this changes
        this.Current = value;
    }
}
