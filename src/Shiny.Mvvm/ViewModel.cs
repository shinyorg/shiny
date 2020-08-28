using System;
using System.ComponentModel;
using System.Threading.Tasks;


namespace Shiny.Mvvm
{
    public abstract class ViewModel : INotifyPropertyChanged, IViewModelLifecycle
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // what about before change?
        // RaiseChanged
        // RaiseIfChanged

        public virtual Task OnPause() => Task.CompletedTask;
        public virtual Task OnResume() => Task.CompletedTask;
        public virtual Task OnStart() => Task.CompletedTask;

        public bool IsBusy { get; protected set; }
    }
}