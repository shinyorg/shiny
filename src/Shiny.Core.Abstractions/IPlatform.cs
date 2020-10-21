using System;


namespace Shiny
{
    public enum PlatformState
    {
        Start,
        Foreground,
        Background
    }


    public interface IPlatform
    {
        //bool Is(string platform);
        //void InvokeOnMainThread(Action action);
        IObservable<PlatformState> WhenStateChanged();
    }
}
