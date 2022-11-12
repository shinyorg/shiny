using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.SpeechRecognition;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechRecognition(this IServiceCollection services)
    {
        services.AddShinyService<SpeechRecognizer>();
        return services;
    }
}

