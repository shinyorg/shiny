using System;
using System.Windows.Input;


namespace Shiny.Integrations.Prism
{
    public class ShinyCommand : IShinyCommand
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
