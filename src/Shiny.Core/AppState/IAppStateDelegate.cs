using System;


namespace Shiny
{
    public interface IAppStateDelegate
    {
        //bool IsAppInForeground { get; }
        void OnStart();
        void OnForeground();
        void OnBackground();
    }
}
