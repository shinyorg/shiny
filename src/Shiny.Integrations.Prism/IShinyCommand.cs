using System;
using System.Windows.Input;


namespace Shiny.Integrations.Prism
{
    public interface IShinyCommand : ICommand
    {
        IObservable<bool> IsExecuting { get; }
    }


    public interface IShinyCommand<TArg> : ICommand
    {
        IObservable<bool> IsExecuting { get; }
    }
}
