using System;
using System.Reactive.Linq;


namespace Shiny.Caching
{
    public abstract class AbstractTimerCache : AbstractCache
    {
        IDisposable timerSub;

        public TimeSpan CleanUpTime { get; set; }

        protected abstract void OnTimerElapsed();


        protected override void Init()
        {
            this.timerSub = Observable
                .Interval(this.CleanUpTime)
                .Synchronize()
                .Subscribe(_ =>
                {
                    try
                    {
                        this.OnTimerElapsed();
                    }
                    catch (Exception ex)
                    {
                    }
                });
        }



        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.timerSub?.Dispose();
        }
    }
}
