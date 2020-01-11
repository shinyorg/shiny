using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreFoundation;
using CoreNFC;
using Foundation;


namespace Shiny.Nfc
{
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/introduction-to-ios11/corenfc
    public class NfcManager : NFCNdefReaderSessionDelegate, INfcManager
    {
        readonly Subject<NDefRecord[]> recordSubj = new Subject<NDefRecord[]>();


        public override void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages)
        {
            foreach (var message in messages)
            {
                var list = new List<NDefRecord>(message.Records.Length);
                foreach (var record in message.Records)
                {
                    list.Add(new NDefRecord
                    {
                        Identifier = record.Identifier.ToArray(),
                        Payload = record.Payload?.ToArray(),
                        Uri = record.WellKnownTypeUriPayload?.ToString(),
                        PayloadType = record.TypeNameFormat switch
                        {
                            NFCTypeNameFormat.AbsoluteUri => NfcPayloadType.Uri,
                            NFCTypeNameFormat.Empty => NfcPayloadType.Empty,
                            NFCTypeNameFormat.NFCExternal => NfcPayloadType.External,
                            NFCTypeNameFormat.NFCWellKnown => NfcPayloadType.WellKnown,
                            NFCTypeNameFormat.Unchanged => NfcPayloadType.Unchanged,
                            //case NFCTypeNameFormat.Media: // TODO: mime?
                            _ => NfcPayloadType.Unknown
                        }
                    });
                }
                this.recordSubj.OnNext(list.ToArray());
            }
        }


        public override void DidInvalidate(NFCNdefReaderSession session, NSError error)
        {
        }


        public IObservable<AccessState> RequestAccess(bool forBroadcasting = false)
        {
            var status = AccessState.Available;
            if (forBroadcasting)
                status = AccessState.NotSupported;

            else if (!NFCNdefReaderSession.ReadingAvailable)
                status = AccessState.Unknown;

            return Observable.Return(status);
        }


        public IObservable<NDefRecord[]> Reader() => Observable.Create<NDefRecord[]>(ob =>
        {
            var session = new NFCNdefReaderSession(this, DispatchQueue.CurrentQueue, true);
            session.BeginSession();

            var sub = this.recordSubj.Subscribe
            (
                ob.OnNext,
                ob.OnError
            );
            return () =>
            {
                session.InvalidateSession();
                sub.Dispose();
            };
        });
    }
}
