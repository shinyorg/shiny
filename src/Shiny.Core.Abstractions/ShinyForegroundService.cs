using System;


namespace Shiny
{
    public class ShinyForegroundService : IShinyStartupTask
    {
        public ShinyForegroundService()
        {
            ShinyHost
                .Resolve<IStartupInitializer>()
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


        public virtual void Start() {}
        public virtual void OnBackground() { }
        public virtual void OnForeground() { }
    }
}
