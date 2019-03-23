using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IAdapterScanner
    {
        bool IsSupported { get; }
        IObservable<ICentralManager> FindAdapters();
    }
}
