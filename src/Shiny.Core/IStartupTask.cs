using System;


namespace Shiny
{
    /// <summary>
    /// Registering this in your Shiny.Startup will run these tasks immediately after the
    /// service container has been built
    /// </summary>
    public interface IStartupTask
    {
        void Start();
    }
}
