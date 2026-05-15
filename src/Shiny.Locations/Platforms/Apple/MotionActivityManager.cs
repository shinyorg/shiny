using System;
using System.Linq;
using System.Threading.Tasks;
using CoreMotion;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public class MotionActivityManager(
    IServiceProvider services,
    ILogger<MotionActivityManager> logger
) : NotifyPropertyChanged, IMotionActivityManager, IShinyStartupTask
{
    readonly CMMotionActivityManager activityManager = new();

    bool isListening;
    public bool IsListening
    {
        get => this.isListening;
        private set => this.Set(ref this.isListening, value);
    }


    public AccessState GetCurrentStatus()
    {
        if (!CMMotionActivityManager.IsActivityAvailable)
            return AccessState.NotSupported;

        return CMMotionActivityManager.AuthorizationStatus switch
        {
            CMAuthorizationStatus.Authorized => AccessState.Available,
            CMAuthorizationStatus.Denied => AccessState.Denied,
            CMAuthorizationStatus.Restricted => AccessState.Restricted,
            _ => AccessState.Unknown
        };
    }


    public Task<AccessState> RequestAccess()
    {
        if (!CMMotionActivityManager.IsActivityAvailable)
            return Task.FromResult(AccessState.NotSupported);

        var status = this.GetCurrentStatus();
        if (status != AccessState.Unknown)
            return Task.FromResult(status);

        var tcs = new TaskCompletionSource<AccessState>();

        // querying activity data triggers the permission prompt
        this.activityManager.QueryActivity(
            NSDate.FromTimeIntervalSinceNow(-1),
            NSDate.Now,
            NSOperationQueue.MainQueue,
            (_, error) =>
            {
                if (error != null)
                    tcs.TrySetResult(AccessState.Denied);
                else
                    tcs.TrySetResult(this.GetCurrentStatus());
            }
        );

        return tcs.Task;
    }


    MotionActivityReading? lastReading;

    public Task<MotionActivityReading?> GetLastReading()
    {
        if (this.lastReading != null)
            return Task.FromResult<MotionActivityReading?>(this.lastReading);

        if (!CMMotionActivityManager.IsActivityAvailable)
            return Task.FromResult<MotionActivityReading?>(null);

        var tcs = new TaskCompletionSource<MotionActivityReading?>();
        this.activityManager.QueryActivity(
            NSDate.FromTimeIntervalSinceNow(-600),
            NSDate.Now,
            NSOperationQueue.MainQueue,
            (activities, error) =>
            {
                if (error != null || activities == null || activities.Length == 0)
                {
                    tcs.TrySetResult(null);
                }
                else
                {
                    var last = activities.Last();
                    tcs.TrySetResult(ToReading(last));
                }
            }
        );
        return tcs.Task;
    }


    public event EventHandler<MotionActivityReading>? MotionActivityReadingReceived;


    public Task StartListener()
    {
        if (this.IsListening)
            throw new InvalidOperationException("Motion activity listener is already running");

        if (!CMMotionActivityManager.IsActivityAvailable)
            throw new InvalidOperationException("Motion activity recognition is not available on this device");

        this.activityManager.StartActivityUpdates(
            NSOperationQueue.MainQueue,
            activity =>
            {
                if (activity == null)
                    return;

                var reading = ToReading(activity);
                this.lastReading = reading;
                this.MotionActivityReadingReceived?.Invoke(this, reading);

                services
                    .RunDelegates<IMotionActivityDelegate>(
                        x => x.OnReading(reading),
                        logger
                    )
                    .ConfigureAwait(false);
            }
        );

        this.IsListening = true;
        return Task.CompletedTask;
    }


    public Task StopListener()
    {
        if (!this.IsListening)
            return Task.CompletedTask;

        this.activityManager.StopActivityUpdates();
        this.IsListening = false;
        return Task.CompletedTask;
    }


    public async void Start()
    {
        if (!this.IsListening)
            return;

        try
        {
            // reset so StartListener doesn't throw
            this.isListening = false;
            await this.StartListener();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to auto-restart motion activity listener");
            this.IsListening = false;
        }
    }


    static MotionActivityReading ToReading(CMMotionActivity activity)
    {
        var type = MotionActivityType.Unknown;

        // priority: Automotive > Cycling > Running > Walking > Stationary
        if (activity.Automotive)
            type = MotionActivityType.Automotive;
        else if (activity.Cycling)
            type = MotionActivityType.Cycling;
        else if (activity.Running)
            type = MotionActivityType.Running;
        else if (activity.Walking)
            type = MotionActivityType.Walking;
        else if (activity.Stationary)
            type = MotionActivityType.Stationary;

        var confidence = activity.Confidence switch
        {
            CMMotionActivityConfidence.High => MotionActivityConfidence.High,
            CMMotionActivityConfidence.Medium => MotionActivityConfidence.Medium,
            _ => MotionActivityConfidence.Low
        };

        var epoch = Convert.ToInt64(activity.StartDate.SecondsSince1970);
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(epoch);
        return new MotionActivityReading(type, confidence, timestamp);
    }
}
