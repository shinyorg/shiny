using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.SpeechRecognition;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseSpeechRecognition(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#elif __IOS__
            if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                builder.AddSingleton<ISpeechRecognizer, SpeechRecognizerImpl>();
                return true;
            }
            return false;
#else
            builder.AddSingleton<ISpeechRecognizer, SpeechRecognizerImpl>();
            return true;
#endif
        }
    }
}
