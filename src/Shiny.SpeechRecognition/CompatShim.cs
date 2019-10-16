using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.SpeechRecognition;


namespace Shiny
{
    public class ShinySpeechRecognitionAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseSpeechRecognition();
        }
    }


    public static class CrossSpeechRecognizer
    {
        public static ISpeechRecognizer Current => ShinyHost.Resolve<ISpeechRecognizer>();
    }
}
