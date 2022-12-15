namespace Sample;


public class MainViewModel : ViewModel
{
    readonly IPushManager pushManager;

    public MainViewModel(IPushManager pushManager)
    {
        this.pushManager = pushManager;
    }


    public bool IsTagsSupported => this.pushManager.Tags != null;
}
