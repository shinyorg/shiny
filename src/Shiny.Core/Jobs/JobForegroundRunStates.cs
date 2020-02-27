using System;


namespace Shiny.Jobs
{
    [Flags]
    public enum JobForegroundRunStates
    {
        None = 0,
        Started = 1,
        Resumed = 2,
        Backgrounded = 4,
        InternetAvailableAny = 8,
        InternetAvailableWifi = 16,
        DeviceCharging = 32
    }
}
