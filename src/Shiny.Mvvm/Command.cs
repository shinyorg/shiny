using System;
using System.Windows.Input;



namespace Shiny.Mvvm
{
    public interface IShinyCommand : ICommand
    {
        IObservable<object> WhenExecuting();
    }


    public class Command : ICommand
    {
        // FromAction, FromTask, FromObservable
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


    public class Command<T> : ICommand
    {
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
