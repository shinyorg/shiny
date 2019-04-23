using System;


namespace Shiny
{
    public class LinuxShinyHost : ShinyHost
    {
        public static void Init(IStartup startup = null, Action<IServiceCollection> platformBuild = null)
        {
        }
    }
}
//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                throw new ArgumentException("This platform plugin is only designed to work on Mono/.NET Core using Linux BlueZ");

//            Current = new Linux.Adapter();
//            AdapterScanner = new Linux.AdapterScanner();