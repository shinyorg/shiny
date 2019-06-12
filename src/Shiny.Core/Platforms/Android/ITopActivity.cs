using System;
using Android.App;

namespace Shiny
{
    public interface ITopActivity
    {
        void Init(Application app);
        Activity Current { get; }
        IObservable<ActivityChanged> WhenActivityStatusChanged();
    }
}
