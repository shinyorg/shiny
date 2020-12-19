using System;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(INavigationService navigationService)
        {
            this.Navigate = ReactiveCommand.CreateFromTask<string>(async arg =>
            {
                this.IsPresented = false;
                await navigationService.Navigate(arg);
            });
        }


        public ICommand Navigate { get; }
        [Reactive] public bool IsPresented { get; set; }


        //public override void OnAppearing()
        //{
        //    base.OnAppearing();
        //    var env = DependencyService.Get<IEnvironment>();
        //    Console.WriteLine($"ENV: {env?.ToString() ?? "NO"}");
        //}
    }
}
