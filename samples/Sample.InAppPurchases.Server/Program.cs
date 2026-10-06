using Sample.InAppPurchases.Server;
using Scalar.AspNetCore;
using Shiny;
using Shiny.InAppPurchases.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services
    // values come from appsettings.json "InAppPurchases" - keep secrets (PrivateKey, ServiceAccountJson) in user-secrets / a vault
    .AddInAppPurchaseServer(options => builder.Configuration.GetSection("InAppPurchases").Bind(options))
    .AddPurchaseEventHandler<LoggingPurchaseEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// POST /iap/apple  -> App Store Connect → App Information → App Store Server Notifications (Version 2)
// POST /iap/google -> Google Cloud Pub/Sub push subscription on the RTDN topic (enable authentication)
app.MapInAppPurchaseWebhooks("/iap");

// POST /iap/verify - the MAUI app sends Purchase.VerificationData here before granting an entitlement.
// Add authentication and chain .RequireAuthorization() in a real app so only signed-in users can call it.
app.MapInAppPurchaseVerification("/iap/verify");

// Ask Apple to send a TEST notification to the URL configured in App Store Connect (needs Server API credentials)
app.MapPost("/iap/apple/test", async (IAppleStoreClient apple, bool? sandbox) =>
    Results.Ok(new { token = await apple.RequestTestNotificationAsync(sandbox ?? true) })
);

app.MapGet("/", () => "Shiny.InAppPurchases sample server");

app.Run();
