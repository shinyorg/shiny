using System;
using System.Threading.Tasks;


namespace Shiny.Mvvm
{
    public interface IGlobalCommandExceptionHandler
    {
        Task OnError(Exception exception);
    }
}
