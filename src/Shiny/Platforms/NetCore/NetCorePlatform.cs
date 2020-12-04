using System;


namespace Shiny
{
    public class NetCorePlatformInitializer : IPlatform
    {
        public void Register(IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Browser))
            {
                // wasm
            }
        }


        public IObservable<PlatformState> WhenStateChanged() => Observable.Empty<PlatformState>();
    }
}
//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                throw new ArgumentException("This platform plugin is only designed to work on Mono/.NET Core using Linux BlueZ");

//            Current = new Linux.Adapter();
//            AdapterScanner = new Linux.AdapterScanner();