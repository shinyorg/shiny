using System;


namespace Shiny.Jobs
{
    [Flags]
    public enum JobForegroundRunStates
    {
        None = 0,
        Started = 1,
        Resumed = 2,
        Backgrounded = 4
    }
}
