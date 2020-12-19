using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Samples.Infrastructure;
using Shiny;


namespace Samples
{
    public static class Extensions
    {
        public static Task PickEnumValue<T>(this IDialogs dialogs, string title, Action<T> action)
        {
            var keys = Enum.GetNames(typeof(T));
            var actions = new List<(string Key, Action Action)>(keys.Length);

            foreach (var key in keys)
            {
                var value = (T)Enum.Parse(typeof(T), key);
                actions.Add((key, () => action(value)));
            }
            return dialogs.ActionSheet(title, false, actions.ToArray());
        }


        public static ICommand PickEnumValueCommand<T>(this IDialogs dialogs, string title, Action<T> action, IObservable<bool>? canExecute = null) =>
            ReactiveCommand.CreateFromTask(() => dialogs.PickEnumValue(title, action), canExecute);


        public static async Task<bool> RequestAccess(this IDialogs dialogs, Func<Task<AccessState>> request)
        {
            var access = await request();
            return await dialogs.AlertAccess(access);
        }


        public static async Task<bool> AlertAccess(this IDialogs dialogs,  AccessState access)
        {
            switch (access)
            {
                case AccessState.Available:
                    return true;

                case AccessState.Restricted:
                    await dialogs.Alert("WARNING: Access is restricted");
                    return true;

                default:
                    await dialogs.Alert("Invalid Access State: " + access);
                    return false;
            }
        }


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
