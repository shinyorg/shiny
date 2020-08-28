using System;
using System.Threading.Tasks;

namespace Shiny.Mvvm.XamarinForms
{
    public class FormsNavigator : INavigator
    {
        public Task GoBack(bool popToRoot, params (string Key, object Value)[] args)
        {
            throw new NotImplementedException();
        }

        public Task GoTo(string uri, params (string Key, object Value)[] args)
        {
            throw new NotImplementedException();
        }

        public IObservable<object> WhenNavigating(string uri, bool to, params (string Key, object Value)[] args)
        {
            throw new NotImplementedException();
        }
    }
}
