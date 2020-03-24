using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.Nfc;


namespace Shiny.Testing.Nfc
{
    public class TestNDefRecord : NDefRecord
    {
        public byte[] Identifier { get; set; }
        public byte[]? Payload { get; set; }
        public string? Uri { get; set; }
        public NfcPayloadType PayloadType { get; set; }
    }


    public class TestNfcManager : INfcManager
    {
        public Subject<NDefRecord[]> ReaderSubject { get; } = new Subject<NDefRecord[]>();
        public IObservable<NDefRecord[]> Reader() => this.ReaderSubject;

        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public IObservable<AccessState> RequestAccess(bool forBroadcasting = false) => Observable.Return(this.RequestAccessReply);
    }
}
