using Shiny.AppFunctions;

namespace Sample.Orders;

// A representative app model. The real generator runs on this file when the test project builds.

public enum Priority { Low, Normal, High }

[AppEntity("customer", Title = "Customer")]
public record Customer(string Id, string Name, string City);

public class CustomerQuery : IAppEntityQuery<Customer>
{
    public static readonly Customer[] All = [new("c1", "Acme", "Toronto"), new("c2", "Globex", "Springfield")];

    public Task<IReadOnlyList<Customer>> GetByIds(IReadOnlyList<string> ids, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Customer>>(All.Where(x => ids.Contains(x.Id)).ToList());

    public Task<IReadOnlyList<Customer>> Search(string text, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Customer>>(All.Where(x => x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task<IReadOnlyList<Customer>> Suggested(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Customer>>(All.Take(1).ToList());
}

public record OrderLine(string Sku, int Quantity);
public record OrderResult(string Number, double Total, Priority Priority, DateTimeOffset Due, IReadOnlyList<OrderLine> Lines, string? Note);

[AppFunction("create_order", Description = "Creates an order for a customer")]
[AppShortcut("Create an order in ${applicationName}", ShortTitle = "New Order", SystemImage = "cart")]
[AppShortcut("Start a ${applicationName} order")]
public record CreateOrder(
    [property: AppParameter(Title = "Customer", Description = "Who the order is for")] Customer Customer,
    int Quantity,
    Priority Priority,
    DateTimeOffset? Due,
    string? Note
) : IAppFunction<OrderResult>
{
    // settable, not in the constructor: still a parameter
    public bool Rush { get; init; }

    // computed: not a parameter
    public string Summary => $"{this.Quantity} for {this.Customer.Name}";
}

public class CreateOrderHandler : IAppFunctionHandler<CreateOrder, OrderResult>
{
    public Task<OrderResult> Handle(CreateOrder request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        context.Say($"Order for {request.Customer.Name} created");
        var due = request.Due ?? new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
        return Task.FromResult(new OrderResult(
            "ORD-1",
            request.Quantity * 9.99,
            request.Rush ? Priority.High : request.Priority,
            due,
            [new("SKU-" + request.Quantity, request.Quantity)],
            request.Note
        ));
    }
}

[AppFunction("count_orders", Title = "Count Orders")]
public record CountOrders : IAppFunction<int>;

public class CountOrdersHandler : IAppFunctionHandler<CountOrders, int>
{
    public Task<int> Handle(CountOrders request, AppFunctionContext context, CancellationToken cancellationToken) => Task.FromResult(42);
}

[AppFunction("cancel_order", OpensApp = true)]
public class CancelOrder : IAppFunction
{
    public required string Number { get; set; }
}

public class CancelOrderHandler : IAppFunctionHandler<CancelOrder>
{
    public static string? LastCancelled;

    public Task Handle(CancelOrder request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        LastCancelled = request.Number;
        return Task.CompletedTask;
    }
}

public class AuditDelegate : IAppFunctionDelegate
{
    public static readonly List<string> Calls = [];

    public Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken cancellationToken)
    {
        lock (Calls)
            Calls.Add(context.FunctionId);
        return Task.FromResult(AppFunctionGate.Allow);
    }
}
