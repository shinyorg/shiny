using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sample.Shared.Maui.Services.Orders;

public enum OrderPriority { Low, Normal, Urgent }

public enum OrderStatus { Open, Shipped, Cancelled }

public record Customer(string Id, string Name, string City);

public record Order(
    string Number,
    string CustomerId,
    string CustomerName,
    int Quantity,
    OrderPriority Priority,
    double Total,
    OrderStatus Status,
    string? Note,
    DateTimeOffset Created
);

/// <summary>
/// The App Functions sample's "backend": customers are fixed, orders are saved to a file so that orders created by
/// Siri or Gemini while the app was closed show up when it opens.
/// </summary>
public class OrderStore
{
    public static readonly Customer[] Customers =
    [
        new("acme", "Acme Corp", "Toronto"),
        new("globex", "Globex", "Springfield"),
        new("initech", "Initech", "Austin"),
        new("umbrella", "Umbrella", "Raccoon City")
    ];

    readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sample-orders.json");
    readonly Lock sync = new();
    List<Order>? orders;

    public event Action? Changed;

    public IReadOnlyList<Order> All
    {
        get
        {
            lock (this.sync)
                return this.Load().OrderByDescending(x => x.Created).ToList();
        }
    }

    public Order Create(Customer customer, int quantity, OrderPriority priority, string? note)
    {
        Order order;
        lock (this.sync)
        {
            var list = this.Load();
            order = new Order(
                $"ORD-{1001 + list.Count}",
                customer.Id,
                customer.Name,
                quantity,
                priority,
                Math.Round(quantity * 19.99, 2),
                OrderStatus.Open,
                note,
                DateTimeOffset.Now
            );
            list.Add(order);
            this.Save(list);
        }
        this.Changed?.Invoke();
        return order;
    }

    public Order? Find(string number)
    {
        lock (this.sync)
            return this.Load().FirstOrDefault(x => x.Number.Equals(number.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Order Cancel(string number)
    {
        Order order;
        lock (this.sync)
        {
            var list = this.Load();
            var index = list.FindIndex(x => x.Number.Equals(number.Trim(), StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                throw new KeyNotFoundException(number);

            order = list[index] = list[index] with { Status = OrderStatus.Cancelled };
            this.Save(list);
        }
        this.Changed?.Invoke();
        return order;
    }

    List<Order> Load()
    {
        if (this.orders == null)
        {
            try
            {
                this.orders = File.Exists(this.path)
                    ? JsonSerializer.Deserialize(File.ReadAllText(this.path), OrderJson.Default.ListOrder) ?? []
                    : [];
            }
            catch (JsonException)
            {
                this.orders = [];
            }
        }
        return this.orders;
    }

    void Save(List<Order> list) => File.WriteAllText(this.path, JsonSerializer.Serialize(list, OrderJson.Default.ListOrder));
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<Order>))]
partial class OrderJson : JsonSerializerContext;
