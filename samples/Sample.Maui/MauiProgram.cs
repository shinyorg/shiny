using Sample.Shared.Maui;
#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .UseSampleShiny();

#if IOS || ANDROID
        // generated: every handler, entity query and delegate in this project (see AppFunctions/) + the runtime
        builder.Services.AddAppFunctions();

        // the same functions as tools for the AI Assistant page - the sign-in delegate gates them there too
        builder.Services.AddAppFunctionAITools(x => x.AddAllFunctions());
#endif

#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        return builder.Build();
    }
}
