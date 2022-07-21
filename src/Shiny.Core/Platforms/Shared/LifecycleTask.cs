using Shiny.Hosting;

namespace Shiny;


#if ANDROID
public partial class ShinyLifecycleTask : IAndroidLifecycle.IApplicationLifecycle, IShinyStartupTask { }

#elif IOS || MACCATALYST
public partial class ShinyLifecycleTask : IIosLifecycle.IApplicationLifecycle, IShinyStartupTask { }

#endif

#if IOS || MACCATALYST || ANDROID
public partial class ShinyLifecycleTask
{
    public virtual void Start() { }

    public bool IsInForeground { get; private set; }
    protected virtual void OnStateChanged(bool backgrounding) { }


    public void OnBackground()
    {
        this.IsInForeground = false;
        this.OnStateChanged(false);
    }


    public virtual void OnForeground()
    {
        this.IsInForeground = true;
        this.OnStateChanged(true);
    }

}


#endif