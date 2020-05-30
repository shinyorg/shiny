using System;
using System.Threading.Tasks;
using System.Timers;
using Shiny.Logging;

namespace Shiny.Caching
{
    public abstract class AbstractTimerCache : AbstractCache, IShinyStartupTask
    {
        readonly Timer timer = new Timer();


        public void Start()
        {
            this.timer.Interval = this.CleanUpTime.TotalMilliseconds;
            this.timer.Elapsed += this.OnTimerElapsed;
            this.timer.Start();
        }


        public TimeSpan CleanUpTime
        {
            get => TimeSpan.FromMilliseconds(this.timer.Interval);
            set => this.timer.Interval = value.TotalMilliseconds;
        }


        protected abstract Task OnTimerElapsed();


        async void OnTimerElapsed(object sender, ElapsedEventArgs args)
        {
            this.timer.Stop();
            try
            {
                await this.OnTimerElapsed().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
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
