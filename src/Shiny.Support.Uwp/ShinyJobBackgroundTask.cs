using System;


namespace Shiny
{
    public sealed class ShinyJobBackgroundTask : AbstractShinyBackgroundTask
    {
        public ShinyJobBackgroundTask() : base("Shiny.UwpShinyHost, Shiny.Core", "BackgroundRun")
        {
        }
    }
}
