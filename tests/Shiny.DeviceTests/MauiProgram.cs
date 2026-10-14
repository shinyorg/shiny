using Microsoft.Maui.DevFlow.Agent;
using Shiny.DeviceTests.Framework;
using Shiny.DeviceTests.Tests;

namespace Shiny.DeviceTests;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddDeviceTestFramework();

        // every module wires its Shiny registration, test delegates, suites and peer commands here
        builder.Services.AddDeviceTestModules();

        builder.AddMauiDevFlowAgent(opts =>
        {
            opts.Port = GetAgentPort();
            DevFlowTestExtension.Register(opts);
        });

        return builder.Build();
    }


    static int GetAgentPort()
    {
#if ANDROID
        return DeviceTestConstants.AndroidAgentPort;
#elif MACCATALYST
        return DeviceTestConstants.MacCatalystAgentPort;
#else
        return DeviceTestConstants.IosAgentPort;
#endif
    }
}
