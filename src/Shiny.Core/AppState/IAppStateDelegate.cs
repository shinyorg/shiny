using System;


namespace Shiny
{
    public interface IAppStateDelegate
    {
        void OnStart();
        void OnForeground();
        void OnBackground();
    }
}
