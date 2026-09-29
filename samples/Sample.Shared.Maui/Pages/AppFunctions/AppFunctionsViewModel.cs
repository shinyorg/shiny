using Microsoft.Maui.ApplicationModel;
using Sample.Shared.Maui.Services.Orders;
using Shiny.AppFunctions;

namespace Sample.Shared.Maui.Pages.AppFunctions;

[ShellMap<AppFunctionsPage>("appfunctions")]
public partial class AppFunctionsViewModel(
    OrderStore store,
    SignInState signIn,
    InvocationLog log,
    AppFunctionDispatcher dispatcher
) : ObservableObject, IPageLifecycleAware
{
    // the functions themselves live in the app head (Sample.Maui/AppFunctions) - the generator only scans the app project
    public string Help { get; } = OperatingSystem.IsIOS()
        ? "Say \"Hey Siri, how many orders are open in Shiny Client\", or search \"New Order\" or \"Open Orders\" in Spotlight."
        : "Gemini calls these on Android 16+. From a shell: adb shell cmd app_function execute-app-function --package org.shinylib.sample --function count_open_orders --parameters '{}'";

    public string Functions { get; } = String.Join(
        Environment.NewLine,
        dispatcher.Registry.Functions.Select(x => $"{x.Id} - {x.Description}")
    );

    [ObservableProperty] bool isSignedIn = signIn.IsSignedIn;
    [ObservableProperty] List<Order> orders = [];
    [ObservableProperty] List<InvocationEntry> calls = [];
    [ObservableProperty] string status = String.Empty;

    partial void OnIsSignedInChanged(bool value) => signIn.IsSignedIn = value;

    public void OnAppearing()
    {
        store.Changed += this.OnChanged;
        log.Added += this.OnChanged;
        this.Refresh();
    }

    public void OnDisappearing()
    {
        store.Changed -= this.OnChanged;
        log.Added -= this.OnChanged;
    }

    // The same pipeline Siri and Gemini go through: binding, delegates, handler, result. Not flagged as
    // foreground, so SignInDelegate's OpenApp gate refuses cancel_order here the way Android does.
    [RelayCommand]
    Task CreateTestOrder() => this.Run("create_order", """{"customer":"acme","quantity":2,"priority":"Normal","note":"from the app"}""");

    [RelayCommand]
    Task CountOpenOrders() => this.Run("count_open_orders", "{}");

    [RelayCommand]
    Task CancelNewestOrder()
    {
        var open = this.Orders.FirstOrDefault(x => x.Status == OrderStatus.Open);
        if (open == null)
        {
            this.Status = "No open orders to cancel";
            return Task.CompletedTask;
        }
        return this.Run("cancel_order", $$"""{"number":"{{open.Number}}"}""");
    }

    [RelayCommand]
    void ClearLog() => log.Clear();

    async Task Run(string functionId, string argumentsJson)
    {
        var outcome = await dispatcher.Execute(new AppFunctionInvocation(functionId), argumentsJson, CancellationToken.None);
        this.Status = outcome.Status == AppFunctionStatus.Success
            ? outcome.Dialog ?? outcome.ResultJson ?? "Done"
            : $"{outcome.ErrorCode}: {outcome.Message}";
    }

    void OnChanged() => MainThread.BeginInvokeOnMainThread(this.Refresh);

    void Refresh()
    {
        this.Orders = store.All.ToList();
        this.Calls = log.Entries.ToList();
    }
}
