using System;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms.Internals;


namespace Shiny
{
    public static class ServicesCollectionExtensions
    {
        public static void UseXamarinFormsDependencyService(this IServiceCollection services)
            => DependencyResolver.ResolveUsing(t => ShinyHost.Container.GetService(t));
    }
}

