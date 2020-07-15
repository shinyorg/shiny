using System;
using System.Reactive;


namespace Shiny.Printers
{
    public interface IPrinter
    {
        string Name { get;  }
        void Queue(string id, string content); // TODO: watch progress on queue?
        void RemoveQueue(string id);
        IObservable<Unit> Print(string content); // TODO: progress?
        IObservable<Unit> Connect();
        void Disconnect();
    }
}
