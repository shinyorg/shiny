using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.SpeechRecognition;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechRecognition(this IServiceCollection services)
    {
        #if PLATFORM
        services.TryAddSingleton<ISpeechRecognizer, SpeechRecognizerImpl>();
        #else
        throw new InvalidOperationException("Invalid Platform");
        #endif
        return services;
    }
}
