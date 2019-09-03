using System;
using Splat;

namespace Shiny.Integrations.ReactiveUI
{
    public static class Extensions
    {
        public static void InstallShiny(this IMutableDependencyResolver locator)
        {
            ShinyHost.Populate((serviceType, func, lifetime) =>
                locator.Register(func, serviceType)
            );
        }


        public static void InstallShiny() => Locator.CurrentMutable.InstallShiny();


        public static IDisposable SubOnMainThread<T>(this IObservable<T> obs, Action<T> onNext)
            => obs
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(onNext);


        public static IDisposable SubOnMainThread<T>(this IObservable<T> obs, Action<T> onNext, Action<Exception> onError)
            => obs
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(onNext, onError);


        public static IDisposable SubOnMainThread<T>(this IObservable<T> obs, Action<T> onNext, Action<Exception> onError, Action onComplete)
            => obs
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(onNext, onError, onComplete);
    }
}
