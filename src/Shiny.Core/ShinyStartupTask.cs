using System;


namespace Shiny
{
    public abstract class ShinyStartupTask : NotifyPropertyChanged, IShinyPersistent, IShinyStartupTask
    {
        public abstract void Start();
    }
}
