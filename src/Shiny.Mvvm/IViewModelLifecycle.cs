using System;
using System.Threading.Tasks;


namespace Shiny.Mvvm
{
    public interface IViewModelLifecycle
    {
        /// <summary>
        /// This is executed before navigating
        /// </summary>
        /// <returns></returns>
        Task OnStart();
        Task OnResume();
        Task OnPause();
    }
}
