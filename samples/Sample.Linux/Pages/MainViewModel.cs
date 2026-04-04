namespace Sample.Linux.Pages;

public partial class MainViewModel : ObservableObject
{
    public List<FeatureItem> Features { get; } =
    [
        new("BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan")
    ];

    [RelayCommand]
    Task Navigate(string route) => Shell.Current.GoToAsync(route);
}

public record FeatureItem(string Title, string Description, string Route);
