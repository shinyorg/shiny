using System;
using System.Threading.Tasks;

namespace Shiny.Locations;


public record MotionActivityTransition(MotionActivityType ActivityType, bool IsEntering);

public interface IAndroidActivityTransitionDelegate
{
    Task OnTransition(MotionActivityTransition transition);
}


public interface IAndroidMotionActivityManager
{
    IObservable<MotionActivityTransition> WhenTransition();

    Task StartTransitionWatch(params MotionActivityType[] typesToWatch);
    Task StopTransitionWatch();
}
