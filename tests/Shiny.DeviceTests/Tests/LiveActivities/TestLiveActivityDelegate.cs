using Shiny.LiveActivities;

namespace Shiny.DeviceTests.Tests;


public class TestLiveActivityDelegate(EventHub events) : LiveActivityDelegate
{
    public const string StartedTopic = "liveactivity.started";
    public const string StateTopic = "liveactivity.state";

    public override Task OnStarted(LiveActivity activity)
    {
        events.Publish(StartedTopic, activity);
        return Task.CompletedTask;
    }

    public override Task OnStateChanged(LiveActivity activity)
    {
        events.Publish(StateTopic, activity);
        return Task.CompletedTask;
    }
}
