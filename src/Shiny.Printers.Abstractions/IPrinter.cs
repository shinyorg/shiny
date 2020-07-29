using System;
using System.Reactive;


namespace Shiny.Printers
{
    public interface IPrinter
    {
        string Name { get;  }
        IObservable<Unit> Print(string content); // TODO: progress?
        IObservable<Unit> Connect();
        void Disconnect();
    }
}
