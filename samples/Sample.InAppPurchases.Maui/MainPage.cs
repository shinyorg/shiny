using System.Collections.ObjectModel;
using Shiny.InAppPurchases;

namespace Sample.InAppPurchases.Maui;


public class MainPage : ContentPage
{
    readonly IInAppPurchaseManager purchases;
    readonly EntitlementService entitlements;
    readonly ObservableCollection<StoreProduct> products = new();
    readonly Label status = new() { FontSize = 13 };
    readonly Label owned = new() { FontSize = 15, FontAttributes = FontAttributes.Bold };
    bool loaded;


    public MainPage(IInAppPurchaseManager purchases, EntitlementService entitlements)
    {
        this.purchases = purchases;
        this.entitlements = entitlements;
        this.Title = "Shiny In-App Purchases";

        var mode = new Label
        {
            FontSize = 12,
            Padding = new Thickness(8, 4),
            TextColor = Colors.White,
            BackgroundColor = PurchaseApi.IsTestMode ? Colors.DarkOrange : Colors.SeaGreen,
            Text = PurchaseApi.IsTestMode
                ? "LOCAL TEST MODE - purchases are not verified by a server"
                : $"Verifying with {SampleConfig.ServerUrl}"
        };

        var list = new CollectionView
        {
            ItemsSource = this.products,
            SelectionMode = SelectionMode.Single,
            EmptyView = new Label { Text = "No products loaded - see readme.md > Troubleshooting", Margin = 16 },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(StoreProduct.Title));

                var desc = new Label { FontSize = 12 };
                desc.SetBinding(Label.TextProperty, nameof(StoreProduct.Description));

                var type = new Label { FontSize = 11, TextColor = Colors.Gray };
                type.SetBinding(Label.TextProperty, nameof(StoreProduct.Type));

                var price = new Label { VerticalOptions = LayoutOptions.Center };
                price.SetBinding(Label.TextProperty, nameof(StoreProduct.DisplayPrice));

                var grid = new Grid
                {
                    Padding = new Thickness(16, 10),
                    ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)]
                };
                grid.Add(new VerticalStackLayout { Children = { title, desc, type } });
                grid.Add(price, 1);
                return grid;
            })
        };
        list.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is StoreProduct product)
            {
                list.SelectedItem = null;
                await this.Buy(product);
            }
        };

        var restore = new Button { Text = "Restore Purchases" };
        restore.Clicked += async (_, _) => await this.Run(async () =>
        {
            // user-initiated only: on iOS this can prompt for the Apple Account password
            var result = await this.purchases.RestorePurchasesAsync();
            await this.entitlements.RefreshAsync(result);
            this.status.Text = result.Count == 0 ? "Nothing to restore" : $"Restored {result.Count} purchase(s)";
        });

        var manage = new Button { Text = "Manage Subscriptions" };
        manage.Clicked += async (_, _) => await this.Run(() => this.purchases.ShowManageSubscriptionsAsync(SampleConfig.Premium));

        var header = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8),
            Spacing = 6,
            Children = { mode, this.owned, this.status }
        };
        var footer = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8),
            Spacing = 8,
            Children = { restore, manage }
        };

        var root = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
        root.Add(header, 0, 0);
        root.Add(list, 0, 1);
        root.Add(footer, 0, 2);
        this.Content = root;

        this.entitlements.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(this.RenderOwned);
        this.purchases.PurchaseUpdated += (_, p) => MainThread.BeginInvokeOnMainThread(() =>
            this.status.Text = $"Update: {p.ProductId} is {p.State}"
        );
        this.RenderOwned();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (this.loaded)
            return;

        await this.Run(async () =>
        {
            if (!await this.purchases.CanMakePaymentsAsync())
            {
                this.status.Text = "Payments are not available (store missing, or restricted by parental controls/MDM)";
                return;
            }

            var result = await this.purchases.GetProductsAsync(SampleConfig.ProductIds);
            foreach (var product in result)
                this.products.Add(product);

            // recover anything paid for but never finished (app killed mid-purchase, approvals while closed)
            await this.entitlements.RefreshAsync();

            this.loaded = true;
            this.status.Text = $"{result.Count} of {SampleConfig.ProductIds.Length} product(s) from {this.purchases.Platform}";
        });
    }


    async Task Buy(StoreProduct product)
    {
        await this.Run(async () =>
        {
            var result = await this.purchases.PurchaseAsync(product.Id, new PurchaseOptions
            {
                AccountToken = EntitlementService.AccountToken
            });

            this.status.Text = result.Status switch
            {
                PurchaseResultStatus.Success => await this.entitlements.ProcessAsync(result.Purchase!)
                    ? $"Purchased {product.Title}"
                    : "Purchase could not be verified",
                PurchaseResultStatus.Pending => "Waiting for approval or payment - you'll get it once it clears",
                PurchaseResultStatus.Cancelled => "Cancelled",
                PurchaseResultStatus.AlreadyOwned => "You already own this - try Restore Purchases",
                _ => result.Status.ToString()
            };
        });
    }


    void RenderOwned() => this.owned.Text =
        $"Coins: {this.entitlements.Coins}   " +
        $"Ads removed: {(this.entitlements.AdsRemoved ? "yes" : "no")}   " +
        $"Premium: {(this.entitlements.IsPremium ? "active" : "no")}";


    async Task Run(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (InAppPurchaseException ex)
        {
            this.status.Text = $"{ex.ErrorCode}: {ex.Message}";
        }
        catch (Exception ex)
        {
            this.status.Text = ex.Message;
        }
    }
}
