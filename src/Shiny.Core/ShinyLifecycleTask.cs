using System;


namespace Shiny
{
    public class ShinyLifecycleTask : ShinyStartupTask
    {
        public ShinyLifecycleTask()
        {
            ShinyHost
                .Resolve<IPlatform>()
                .WhenStateChanged()
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case PlatformState.Background:
                            this.OnBackground();
                            this.IsInForeground = false;
                            break;

                        case PlatformState.Foreground:
                            this.OnForeground();
                            this.IsInForeground = true;
                            break;
                    }
                });
        }


        protected bool IsInForeground { get; private set; }
        public override void Start() {}
        public virtual void OnBackground() { }
        public virtual void OnForeground() { }
    }
}
