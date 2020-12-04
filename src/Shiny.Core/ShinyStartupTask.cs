using System;


namespace Shiny
{
    public abstract class ShinyStartupTask : NotifyPropertyChanged, IShinyStartupTask
    {
        public abstract void Start();
    }
}
