using System;
using System.Threading.Tasks;
using System.Timers;


namespace Shiny.Caching
{
    public abstract class AbstractTimerCache : AbstractCache
    {
        readonly Timer timer;


        protected AbstractTimerCache()
        {
            this.timer = new Timer();
        }


        public TimeSpan CleanUpTime
        {
            get => TimeSpan.FromMilliseconds(this.timer.Interval);
            set => this.timer.Interval = value.TotalMilliseconds;
        }


        protected abstract Task OnTimerElapsed();


        protected override void Init()
        {
            this.timer.Interval = this.CleanUpTime.TotalMilliseconds;
            this.timer.Elapsed += this.OnTimerElapsed;
            this.timer.Start();
        }


        async void OnTimerElapsed(object sender, ElapsedEventArgs args)
        {
            this.timer.Stop();
            try
            {
                await this.OnTimerElapsed().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO?
            }
            this.timer.Start();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.timer.Elapsed -= this.OnTimerElapsed;
            this.timer.Dispose();
        }
    }
}
