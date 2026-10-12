using Sample.Shared.Maui.Services;
using Shiny.Wearables;

namespace Sample.Shared.Maui.Pages.Wearables;

[ShellMap<WearablesPage>("wearables")]
public partial class WearablesViewModel(
    IWearableManager wearables,
    IEventStore events,
    IPlatform platform
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] bool isSupported;
    [ObservableProperty] bool isPaired;
    [ObservableProperty] bool isAppInstalled;
    [ObservableProperty] bool isReachable;
    [ObservableProperty] int pendingTransfers;
    [ObservableProperty] List<WearableNodeItem> nodes = [];
    [ObservableProperty] string path = "sample";
    [ObservableProperty] string text = "Hello from the phone";
    [ObservableProperty] string result = "-";
    [ObservableProperty] string lastReceived = "Nothing yet";
    [ObservableProperty] string receivedContext = "-";

    public void OnAppearing()
    {
        events.EventAdded += this.OnEventAdded;
        _ = this.Refresh();
    }

    public void OnDisappearing() => events.EventAdded -= this.OnEventAdded;

    // SampleWearableDelegate records everything the watch sends, including while this page is closed
    void OnEventAdded(object? sender, EventRecord e)
    {
        if (e.Category != SampleWearableDelegate.Category)
            return;

        platform.InvokeOnMainThread(() =>
        {
            this.LastReceived = $"{e.Timestamp.LocalDateTime:HH:mm:ss} {e.Description}";
            _ = this.Refresh();
        });
    }

    [RelayCommand]
    async Task Refresh()
    {
        try
        {
            var status = await wearables.GetStatus();
            this.IsSupported = status.IsSupported;
            this.IsPaired = status.IsPaired;
            this.IsAppInstalled = status.IsAppInstalled;
            this.IsReachable = status.IsReachable;
            this.Nodes = status
                .Nodes
                .Select(x => new WearableNodeItem(
                    x.DisplayName,
                    $"{(x.IsConnected ? "Connected" : "Disconnected")} · {(x.IsNearby ? "Nearby" : "Cloud")} · {(x.HasApp ? "App installed" : "No app")}"
                ))
                .ToList();

            if (status.IsSupported)
            {
                var pending = await wearables.GetPendingTransfers();
                this.PendingTransfers = pending.Count;

                var context = await wearables.GetReceivedContext();
                this.ReceivedContext = context == null ? "-" : context.GetString();
            }
        }
        catch (Exception ex)
        {
            this.Result = "Error: " + ex.Message;
        }
    }

    [RelayCommand]
    Task SendMessage() => this.Run(async () =>
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var reply = await wearables.SendMessage(this.Path, this.Text, cancelToken: cts.Token);
        return reply.Length == 0 ? "Sent (no reply)" : "Reply: " + reply;
    });

    [RelayCommand]
    Task UpdateContext() => this.Run(async () =>
    {
        await wearables.UpdateContext(new Dictionary<string, object>
        {
            ["text"] = this.Text,
            ["sentAt"] = DateTimeOffset.Now
        });
        return "Context updated - the watch gets the latest copy when it next wakes";
    });

    [RelayCommand]
    Task Transfer() => this.Run(async () =>
    {
        var id = await wearables.Transfer(this.Path, this.Text);
        return $"Transfer {id} queued";
    });

    async Task Run(Func<Task<string>> action)
    {
        try
        {
            this.Result = "Working...";
            this.Result = await action();
            await this.Refresh();
        }
        catch (WearableException ex)
        {
            this.Result = $"{ex.Code}: {ex.Message}";
        }
        catch (Exception ex)
        {
            this.Result = "Error: " + ex.Message;
        }
    }
}

public record WearableNodeItem(string Title, string Description);
