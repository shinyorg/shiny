using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.SpeechRecognition;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddSpeechRecognition(this IServiceCollection services)
        {
#if IOS || MACCATALYST
            services.TryAddSingleton<ISpeechRecognizer, SpeechRecognizerImpl>();
#elif ANDROID
            services.TryAddSingleton<ISpeechRecognizer, SpeechRecognizerImpl>();
#else
            throw new InvalidOperationException("This platform is not supported");
#endif

            return services;
        }
    }
}
