using System;
using System.Reactive;


namespace Shiny.Printers.Zebra
{
    public class ZebraPrinter : IPrinter
    {
        public string Name => throw new NotImplementedException();

        public IObservable<Unit> Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Print(string content)
        {
            throw new NotImplementedException();
        }
    }
}
