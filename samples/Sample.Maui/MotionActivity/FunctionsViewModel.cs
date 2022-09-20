using Shiny;
using Shiny.Locations;

namespace Sample.MotionActivity;


public class FunctionsViewModel : ViewModel
{
    readonly IMotionActivityManager activityManager;


    public FunctionsViewModel(BaseServices services, IMotionActivityManager activityManager) : base(services)
    {
        this.activityManager = activityManager;
    }


    public override Task InitializeAsync(INavigationParameters parameters) 
    {

        //this.activityManager
        //    .WhenActivityChanged()
        //    .SubOnMainThread(
        //        x => this.CurrentActivity = $"({x.Confidence}) {x.Types}",
        //        ex => { } // TODO
        //    )
        //    .DisposedBy(this.DestroyWith);

        // TODO: use observable on manager
        Observable
            .Interval(TimeSpan.FromSeconds(5))
            .SubOnMainThread(async _ =>
            {
                try
                {
                    var current = await this.activityManager.GetCurrentActivity();
                    if (current == null)
                    {
                        this.CurrentText = "No current activity found - this should not happen";
                        this.IsCurrentAuto = false;
                        this.IsCurrentStationary = false;
                    }
                    else
                    {
                        this.CurrentText = $"{current.Types} ({current.Confidence})";

                        this.IsCurrentAuto = await this.activityManager.IsCurrentAutomotive();
                        this.IsCurrentStationary = await this.activityManager.IsCurrentStationary();
                    }
                }
                catch (Exception ex)
                {
                    await this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK");
                }
            })
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }


    [Reactive] public string CurrentText { get; private set; }
    [Reactive] public bool IsCurrentAuto { get; private set; }
    [Reactive] public bool IsCurrentStationary { get; private set; }
}
