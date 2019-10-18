using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;

[assembly: Shiny.ShinySpeechRecognitionAutoRegister]


namespace Shiny
{
    public class ShinySpeechRecognitionAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseSpeechRecognition();
        }
    }


    public class ShinySpeechRecognitionAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseSpeechRecognition();
        }
    }
}
