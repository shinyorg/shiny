using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shiny.InAppPurchases.Server;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server
{
    public sealed class InAppPurchaseServerBuilder(IServiceCollection services)
    {
        public IServiceCollection Services { get; } = services;


        /// <summary>
        /// Adds a handler for verified store notifications (scoped). Multiple handlers run in registration order.
        /// </summary>
        public InAppPurchaseServerBuilder AddPurchaseEventHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
            where THandler : class, IPurchaseEventHandler
        {
            this.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPurchaseEventHandler, THandler>());
            return this;
        }


        /// <summary>
        /// Replaces the in-memory de-duplicator - use shared storage (database, Redis) when running multiple instances.
        /// </summary>
        public InAppPurchaseServerBuilder UseDeduplicator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDeduplicator>()
            where TDeduplicator : class, IPurchaseEventDeduplicator
        {
            this.Services.RemoveAll<IPurchaseEventDeduplicator>();
            this.Services.AddSingleton<IPurchaseEventDeduplicator, TDeduplicator>();
            return this;
        }
    }
}


namespace Shiny
{
    public static class InAppPurchaseServerServiceCollectionExtensions
    {
        /// <summary>
        /// Registers App Store / Google Play notification processing, <see cref="IAppleStoreClient"/>,
        /// <see cref="IGooglePlayClient"/> and <see cref="IPurchaseVerifier"/>. Options are validated at startup.
        /// </summary>
        public static InAppPurchaseServerBuilder AddInAppPurchaseServer(this IServiceCollection services, Action<InAppPurchaseServerOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.AddOptions<InAppPurchaseServerOptions>().Configure(configure).ValidateOnStart();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InAppPurchaseServerOptions>, InAppPurchaseServerOptionsValidator>());
            services.TryAddSingleton(TimeProvider.System);

            services.AddHttpClient(AppStoreClient.HttpClientName);
            services.AddHttpClient(GooglePlayClient.HttpClientName);

            services.TryAddSingleton<IAppleStoreClient, AppStoreClient>();
            services.TryAddSingleton<GoogleAccessTokenProvider>();
            services.TryAddSingleton<IGooglePlayClient, GooglePlayClient>();
            services.TryAddSingleton<GooglePubSubAuthenticator>();
            services.TryAddSingleton<IPurchaseEventDeduplicator, InMemoryPurchaseEventDeduplicator>();
            services.TryAddSingleton<IPurchaseVerifier, PurchaseVerifier>();
            services.TryAddScoped<InAppPurchaseWebhookProcessor>();

            return new InAppPurchaseServerBuilder(services);
        }
    }


    public static class InAppPurchaseServerEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps <c>POST {prefix}/apple</c> (App Store Server Notifications V2 - set this URL in App Store Connect) and
        /// <c>POST {prefix}/google</c> (Pub/Sub push endpoint for Real-time Developer Notifications).
        /// Both endpoints authenticate the caller themselves (Apple JWS chain / Google OIDC token), so they are marked
        /// AllowAnonymous and must stay reachable without your app's authentication.
        /// </summary>
        public static RouteGroupBuilder MapInAppPurchaseWebhooks(this IEndpointRouteBuilder endpoints, string prefix = "/iap")
        {
            var group = endpoints.MapGroup(prefix);
            group.MapPost("/apple", (RequestDelegate)HandleApple).WithDisplayName("Shiny In-App Purchases - App Store Server Notifications");
            group.MapPost("/google", (RequestDelegate)HandleGoogle).WithDisplayName("Shiny In-App Purchases - Google Play RTDN");
            group.AllowAnonymous();
            return group;
        }


        /// <summary>
        /// Maps a POST endpoint that verifies a purchase reported by the app. Body:
        /// <c>{ "platform": "AppStore" | "GooglePlay", "verificationData": "...", "productId": "..." }</c>, response: <see cref="VerifiedPurchase"/>.
        /// This endpoint is NOT protected - chain <c>.RequireAuthorization()</c> so only your signed-in users can call it,
        /// and grant entitlements to the authenticated user rather than trusting the request.
        /// </summary>
        public static IEndpointConventionBuilder MapInAppPurchaseVerification(this IEndpointRouteBuilder endpoints, string pattern = "/iap/verify")
            => endpoints.MapPost(pattern, (RequestDelegate)HandleVerify).WithDisplayName("Shiny In-App Purchases - Verify Purchase");


        static Task HandleApple(HttpContext context) => context.RequestServices.GetRequiredService<InAppPurchaseWebhookProcessor>().HandleAppleAsync(context);
        static Task HandleGoogle(HttpContext context) => context.RequestServices.GetRequiredService<InAppPurchaseWebhookProcessor>().HandleGoogleAsync(context);
        static Task HandleVerify(HttpContext context) => context.RequestServices.GetRequiredService<InAppPurchaseWebhookProcessor>().HandleVerifyAsync(context);
    }
}
