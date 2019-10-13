using System;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public static class CrossJobManager
    {
        public static IJobManager Current => ShinyHost.Resolve<IJobManager>();
    }


    public static class CrossFileSystem
    {
        public static IFileSystem Current => ShinyHost.Resolve<IFileSystem>();
    }


    public static class CrossPower
    {
        public static IPowerManager Current => ShinyHost.Resolve<IPowerManager>();
    }


    public static class CrossConnectivity
    {
        public static IConnectivity Current => ShinyHost.Resolve<IConnectivity>();
    }


    public static class CrossSettings
    {
        public static ISettings Current => ShinyHost.Resolve<ISettings>();
    }


    public static class CrossEnvironment
    {
        public static IEnvironment Current => ShinyHost.Resolve<IEnvironment>();
    }
}
