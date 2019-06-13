using System;
using Android.App;

namespace Shiny
{
    public interface ITopActivity
    {
        Activity Current { get; }
        IObservable<ActivityChanged> WhenActivityStatusChanged();
    }
}
