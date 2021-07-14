using System;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(INavigationService navigator)
        {
            this.Navigate = ReactiveCommand.CreateFromTask<string>(async arg =>
            {
                this.IsPresented = false;
                await navigator.Navigate(arg);
            });
        }


        public ICommand Navigate { get; }
        [Reactive] public bool IsPresented { get; set; }
    }
}
