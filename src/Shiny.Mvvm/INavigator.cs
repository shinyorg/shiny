using System;
using System.Threading.Tasks;


namespace Shiny.Mvvm
{
    public interface INavigator
    {
        Task GoBack(bool popToRoot, params (string Key, object Value)[] args);
        Task GoTo(string uri, params (string Key, object Value)[] args);

        IObservable<object> WhenNavigating(string uri, bool to, params (string Key, object Value)[] args);
    }
}
