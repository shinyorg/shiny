using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.Nfc;


namespace Shiny.Testing.Nfc
{
    public class TestNDefRecord : INDefRecord
    {
        public byte[] Identifier { get; set; }
        public byte[]? Payload { get; set; }
        public string? Uri { get; set; }
        public NfcPayloadType PayloadType { get; set; }
    }


    public class TestNfcManager : INfcManager
    {
        public Subject<INDefRecord[]> ReaderSubject { get; } = new Subject<INDefRecord[]>();
        public IObservable<INDefRecord[]> Reader() => this.ReaderSubject;

        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public IObservable<AccessState> RequestAccess(bool forBroadcasting = false) => Observable.Return(this.RequestAccessReply);
    }
}
