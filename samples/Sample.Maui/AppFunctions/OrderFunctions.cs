using Sample.Shared.Maui.Services.Orders;
using Shiny.AppFunctions;

namespace Sample.Maui.AppFunctions;

// Everything Siri and Gemini can do with this app. AddAppFunctions() (source-generated) registers all of it.
// These have to live in the app project - the generator only scans the app. Plain types such as OrderStore and the
// OrderPriority enum can come from any referenced library (here Sample.Shared.Maui).

[AppEntity("customer", Title = "Customer")]
public record CustomerEntity(string Id, string Name, string City)
{
    public static CustomerEntity From(Customer customer) => new(customer.Id, customer.Name, customer.City);
    public Customer ToCustomer() => new(this.Id, this.Name, this.City);
}

public class CustomerQuery : IAppEntityQuery<CustomerEntity>
{
    public Task<IReadOnlyList<CustomerEntity>> GetByIds(IReadOnlyList<string> ids, CancellationToken cancellationToken)
        => Result(OrderStore.Customers.Where(x => ids.Contains(x.Id)));

    public Task<IReadOnlyList<CustomerEntity>> Search(string text, CancellationToken cancellationToken)
        => Result(OrderStore.Customers.Where(x =>
            x.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            x.City.Contains(text, StringComparison.OrdinalIgnoreCase)
        ));

    public Task<IReadOnlyList<CustomerEntity>> Suggested(CancellationToken cancellationToken)
        => Result(OrderStore.Customers);

    static Task<IReadOnlyList<CustomerEntity>> Result(IEnumerable<Customer> customers)
        => Task.FromResult<IReadOnlyList<CustomerEntity>>(customers.Select(CustomerEntity.From).ToList());
}

public record OrderResult(string Number, string Customer, int Quantity, OrderPriority Priority, double Total, string Status);


[AppFunction("create_order", Description = "Creates an order for a customer")]
[AppShortcut("Create an order in ${applicationName}", ShortTitle = "New Order", SystemImage = "cart.badge.plus")]
public record CreateOrder(
    [property: AppParameter(Title = "Customer", Description = "Who the order is for")] CustomerEntity Customer,
    [property: AppParameter(Title = "Quantity", Description = "How many units")] int Quantity,
    OrderPriority Priority,
    string? Note
) : IAppFunction<OrderResult>;

public class CreateOrderHandler(OrderStore store) : IAppFunctionHandler<CreateOrder, OrderResult>
{
    public Task<OrderResult> Handle(CreateOrder request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        if (request.Quantity is < 1 or > 1000)
            throw new AppFunctionException(AppFunctionErrorCode.InvalidArgument, "Quantity must be between 1 and 1000");

        var order = store.Create(request.Customer.ToCustomer(), request.Quantity, request.Priority, request.Note);
        context.Say($"Order {order.Number} for {order.CustomerName} is in: {order.Quantity} units, {order.Total:C}.");
        return Task.FromResult(order.ToResult());
    }
}


[AppFunction("count_open_orders", Title = "Open Orders", Description = "Counts the orders that have not shipped or been cancelled")]
[AppShortcut("How many orders are open in ${applicationName}", SystemImage = "shippingbox")]
[AppShortcut("Open orders in ${applicationName}")]
public record CountOpenOrders : IAppFunction<int>;

public class CountOpenOrdersHandler(OrderStore store) : IAppFunctionHandler<CountOpenOrders, int>
{
    public Task<int> Handle(CountOpenOrders request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        var count = store.All.Count(x => x.Status == OrderStatus.Open);
        context.Say(count switch
        {
            0 => "There are no open orders.",
            1 => "There is 1 open order.",
            _ => $"There are {count} open orders."
        });
        return Task.FromResult(count);
    }
}


[AppFunction("order_status", Description = "Looks up an order by its number, for example ORD-1001")]
public record OrderStatusRequest([property: AppParameter(Title = "Order Number")] string Number) : IAppFunction<OrderResult>;

public class OrderStatusHandler(OrderStore store) : IAppFunctionHandler<OrderStatusRequest, OrderResult>
{
    public Task<OrderResult> Handle(OrderStatusRequest request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        var order = store.Find(request.Number)
            ?? throw new AppFunctionException(AppFunctionErrorCode.NotFound, $"There is no order {request.Number}");

        context.Say($"{order.Number} for {order.CustomerName} is {order.Status.ToString().ToLowerInvariant()}.");
        return Task.FromResult(order.ToResult());
    }
}


[AppFunction("cancel_order", Description = "Cancels an open order. Needs a signed-in user.")]
public record CancelOrder([property: AppParameter(Title = "Order Number")] string Number) : IAppFunction;

public class CancelOrderHandler(OrderStore store) : IAppFunctionHandler<CancelOrder>
{
    public Task Handle(CancelOrder request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        var order = store.Find(request.Number)
            ?? throw new AppFunctionException(AppFunctionErrorCode.NotFound, $"There is no order {request.Number}");

        if (order.Status != OrderStatus.Open)
            throw new AppFunctionException(AppFunctionErrorCode.InvalidArgument, $"{order.Number} is already {order.Status.ToString().ToLowerInvariant()}");

        store.Cancel(order.Number);
        context.Say($"{order.Number} is cancelled.");
        return Task.CompletedTask;
    }
}


/// <summary>
/// Cancelling needs a signed-in user. From the background, Siri asks to continue in the app (where you can sign in)
/// and Android refuses; once the app is on screen, OpenApp would pass, so the call is refused until you sign in.
/// </summary>
public class SignInDelegate(SignInState signIn) : IAppFunctionDelegate
{
    public Task<AppFunctionGate> OnInvoking(AppFunctionContext context, CancellationToken cancellationToken)
    {
        if (context.FunctionId == "cancel_order" && !signIn.IsSignedIn)
            return Task.FromResult(context.IsForeground
                ? AppFunctionGate.Deny("Sign in to cancel orders.")
                : AppFunctionGate.OpenApp("Sign in to cancel orders."));

        return Task.FromResult(AppFunctionGate.Allow);
    }
}

/// <summary>Records every call for the log on the App Functions page.</summary>
public class LoggingDelegate(InvocationLog log) : IAppFunctionDelegate
{
    public Task OnInvoked(AppFunctionContext context, object? result, Exception? exception)
    {
        var from = context.Platform == AppFunctionPlatform.Android && !String.IsNullOrEmpty(context.CallerPackage)
            ? $" from {context.CallerPackage}"
            : "";
        var outcome = exception != null ? $"failed: {exception.Message}" : context.Dialog ?? "ok";
        log.Add($"[{context.Platform}{(context.IsForeground ? ", foreground" : "")}] {context.FunctionId}{from} → {outcome}");
        return Task.CompletedTask;
    }
}

static class OrderExtensions
{
    public static OrderResult ToResult(this Order order)
        => new(order.Number, order.CustomerName, order.Quantity, order.Priority, order.Total, order.Status.ToString());
}
