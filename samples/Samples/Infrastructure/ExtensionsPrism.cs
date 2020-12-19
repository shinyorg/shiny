using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;


namespace Samples
{
    public static class ExtensionsPrism
    {
        public static Task ShowBigText(this INavigationService navigator, string text, string? title = null)
            => navigator.Navigate("BigText", ("Text", text), ("Title", title));


        public static Task Navigate(this INavigationService navigation, string uri, params (string, object)[] parameters)
            => navigation.Navigate(uri, parameters.ToNavParams());


        public static async Task Navigate(this INavigationService navigation, string uri, INavigationParameters parameters)
        {
            var result = await navigation.NavigateAsync(uri, parameters);
            if (!result.Success)
                Console.WriteLine("[NAV FAIL] " + result.Exception);
            //throw new ArgumentException("Failed to navigate", result.Exception);
        }


        public static ICommand NavigateCommand(this INavigationService navigation, string uri, Action<INavigationParameters> getParams = null, IObservable<bool> canExecute = null)
            => ReactiveCommand.CreateFromTask(async () =>
            {
                var p = new NavigationParameters();
                getParams?.Invoke(p);
                await navigation.Navigate(uri, p);
            }, canExecute);


        public static ICommand NavigateCommand<T>(this INavigationService navigation, string uri, Action<T, INavigationParameters> getParams = null, IObservable<bool> canExecute = null)
            => ReactiveCommand.CreateFromTask<T>(async arg =>
            {
                var p = new NavigationParameters();
                getParams?.Invoke(arg, p);
                await navigation.Navigate(uri, p);
            }, canExecute);



        public static Task GoBack(this INavigationService navigation, bool toRoot = false, params (string, object)[] parameters)
            => navigation.GoBack(toRoot, parameters.ToNavParams());


        public static async Task GoBack(this INavigationService navigation, bool toRoot = false, INavigationParameters parameters = null)
        {
            parameters = parameters ?? new NavigationParameters();
            var task = toRoot
                ? navigation.GoBackToRootAsync(parameters)
                : navigation.GoBackAsync(parameters);

            var result = await task.ConfigureAwait(false);
            if (!result.Success)
                Console.WriteLine("[NAV FAIL] " + result.Exception);
        }


        public static ICommand GoBackCommand(this INavigationService navigation, bool toRoot = false, Action<INavigationParameters> getParams = null, IObservable<bool> canExecute = null)
            => ReactiveCommand.CreateFromTask(async () =>
            {
                var p = new NavigationParameters();
                getParams?.Invoke(p);
                await navigation.GoBack(toRoot, p);
            }, canExecute);


        public static ICommand GoBackCommand<T>(this INavigationService navigation, bool toRoot = false, Action<T, INavigationParameters> getParams = null, IObservable<bool> canExecute = null)
            => ReactiveCommand.CreateFromTask<T>(async arg =>
            {
                var p = new NavigationParameters();
                getParams?.Invoke(arg, p);
                await navigation.GoBack(toRoot, p);
            }, canExecute);


        public static INavigationParameters Set(this INavigationParameters parameters, string key, object value)
        {
            parameters.Add(key, value);
            return parameters;
        }


        public static INavigationParameters AddRange(this INavigationParameters parameters, params (string Key, object Value)[] args)
        {
            foreach (var arg in args)
                parameters.Add(arg.Key, arg.Value);

            return parameters;
        }


        static NavigationParameters ToNavParams(this (string Key, object Value)[] parameters)
        {

            var navParams = new NavigationParameters();
            if (parameters != null && parameters.Any())
                navParams.AddRange(parameters);

            return navParams;
        }
    }
}
