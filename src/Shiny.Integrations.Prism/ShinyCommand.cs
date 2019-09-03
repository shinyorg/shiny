using System;
using System.Threading.Tasks;
using System.Windows.Input;


namespace Shiny.Integrations.Prism
{
    public class ShinyCommand : IShinyCommand
    {
        public static IShinyCommand Create(Action action, IObservable<bool> canExecute = null)
        {
            return null;
        }


        public static IShinyCommand<TArg> Create<TArg>(Action<TArg> action, IObservable<bool> canExecute = null)
        {
            return null;
        }


        public static IShinyCommand CreateFromTask(Func<Task> task, IObservable<bool> canExecute = null)
        {
            return null;
        }


        public static IShinyCommand<TArg> CreateFromTask<TArg>(Func<TArg, Task> task, IObservable<bool> canExecute = null)
        {
            return null;
        }

        public IObservable<bool> IsExecuting => throw new NotImplementedException();

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}
