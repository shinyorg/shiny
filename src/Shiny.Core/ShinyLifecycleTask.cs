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
                            break;

                        case PlatformState.Foreground:
                            this.OnForeground();
                            break;
                    }
                });
        }


        public override void Start() {}
        public virtual void OnBackground() { }
        public virtual void OnForeground() { }
    }
}
