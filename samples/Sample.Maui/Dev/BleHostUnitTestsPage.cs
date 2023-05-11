using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace Sample.Dev;


public class BleHostUnitTestsPage : ContentPage
{
    public BleHostUnitTestsPage()
    {
        this.ToolbarItems.Add(new ToolbarItem()
            .Text("Clear")
            .BindCommand(static (BleHostUnitTestsViewModel vm) => vm.Clear)
        );
        
        this.Content = new Grid
        {
            RowDefinitions = Rows.Define(
                GridLength.Auto,
                GridLength.Star
            ),

            ColumnDefinitions = Columns.Define(
                new(1, GridUnitType.Star),
                new(1, GridUnitType.Star)
            ),

            Children =
            {
                new Button()
                    .Text("Start Server")
                    .Row(0)
                    .Column(0)
                    .BindCommand(static (BleHostUnitTestsViewModel vm) => vm.StartServer),

                new Button()
                    .Text("Stop Server")
                    .Row(0)
                    .Column(1)
                    .BindCommand(static (BleHostUnitTestsViewModel vm) => vm.StopServer),

                new CollectionView()
                    .Row(1)
                    .ColumnSpan(2)
                    .EmptyView(new EmptyView("No BLE Host Logs Yet"))
                    .ItemTemplate(new DataTemplate(() => new BleHostItem()))
                    .Fill()
                    .Bind(
                        CollectionView.ItemsSourceProperty,
                        static (BleHostUnitTestsViewModel vm) => vm.Logs
                    )
            }
        };
    }
}


public class BleHostItem : Grid
{
    public BleHostItem()
    {
        this.Margin = 5;
        this.Padding = 5;

        this.RowDefinitions = Rows.Define(
            GridLength.Auto,
            GridLength.Auto,
            GridLength.Auto
        );
        this.ColumnDefinitions = Columns.Define(
            new(1, GridUnitType.Star),
            new(1, GridUnitType.Star)
        );

        this.Children.Add(
            new Label()
                .Row(0)
                .Column(0)
                .Bind(
                    Label.TextProperty,
                    static (BleLog log) => log.Event
                )
        );
        this.Children.Add(
            new Label()
                .Row(0)
                .Column(1)
                .TextEnd()
                .Bind(
                    Label.TextProperty,
                    static (BleLog log) => log.Timestamp
                )
        );
        this.Children.Add(
            new Label()
                .Row(1)
                .ColumnSpan(2)
                .Font(italic: true)                
                .Bind(
                    Label.TextProperty,
                    static (BleLog log) => log.Data
                )
        );
        this.Children.Add(
            new Hr().Row(2).ColumnSpan(2)
        );
    }
}

public class EmptyView : Label
{
    public EmptyView(string message)
    {
        this.Fill()
            .FontSize(20)
            .TextCenter()
            .Text(message);
    }
}

public class Hr : BoxView
{
    public Hr()
    {
        this.Color = Colors.LightGray;
        this.HeightRequest = 1;
        this.FillHorizontal()
            .CenterVertical();
    }
}