namespace Shiny.DeviceTests.Framework;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeviceTestFramework(this IServiceCollection services)
    {
        services.AddSingleton<EventHub>();
        services.AddSingleton<TestRunner>();
        services.AddSingleton<PeerCommandDispatcher>();
        return services;
    }


    public static IServiceCollection AddDeviceTestSuite<TSuite>(this IServiceCollection services) where TSuite : DeviceTestSuite
    {
        services.AddTransient<DeviceTestSuite, TSuite>();
        return services;
    }


    public static IServiceCollection AddPeerCommand<TCommand>(this IServiceCollection services) where TCommand : class, IPeerCommand
    {
        services.AddSingleton<IPeerCommand, TCommand>();
        return services;
    }
}
