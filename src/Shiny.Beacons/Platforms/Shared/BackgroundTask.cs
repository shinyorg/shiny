#if WINDOWS_UWP || __ANDROID__
using System;
using Shiny.Infrastructure;
using Shiny.BluetoothLE.Central;


namespace Shiny.Beacons
{
    public class BackgroundTask
    {
        readonly object syncLock;
        readonly ICentralManager centralManager;
        readonly IRepository repository;


        public BackgroundTask(ICentralManager centralManager, IRepository repository)
        {
            this.syncLock = new object();
            this.centralManager = centralManager;
            this.repository = repository;

            //this.repository.Added += (sender, args) => { };
            //this.repository.Removed += (sender, args) => { };
            //this.repository.Cleared += (sender, args) => { };
        }


        public void Run()
        {
        }
    }
}
#endif