using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using CoreNFC;
using Foundation;


namespace Shiny.Nfc
{
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/introduction-to-ios11/corenfc
    public class NfcManager : NFCNdefReaderSessionDelegate, INfcManager
    {
        readonly Subject<NDefRecord[]> recordSubj = new Subject<NDefRecord[]>();
        readonly Subject<NSError> invalidSubj = new Subject<NSError>(); 


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
            => Task.Run(() => this.invalidSubj.OnNext(error));


        public IObservable<AccessState> RequestAccess()
        {
            var status = AccessState.Available;

            if (!NFCNdefReaderSession.ReadingAvailable)
                status = AccessState.NotSupported;

            return Observable.Return(status);
        }


        
        public IObservable<NDefRecord[]> SingleRead() => this.DoRead(true);
        public IObservable<NDefRecord[]> ContinuousRead() => this.DoRead(false);


        protected virtual IObservable<NDefRecord[]> DoRead(bool singleRead) => Observable.Create<NDefRecord[]>(ob =>
        {
            var session = new NFCNdefReaderSession(this, DispatchQueue.CurrentQueue, true);
            var cancel = false;


            var invalidSub = this.invalidSubj.Subscribe(x =>
            {
                switch (x.Code)
                {
                    case 200:
                        ob.OnCompleted();
                        break;

                    case 203:
                        if (!cancel)
                        {
                            Task.Delay(500).ContinueWith(_ =>
                            {
                                if (!cancel)
                                {
                                    session = new NFCNdefReaderSession(this, DispatchQueue.CurrentQueue, true);
                                    session.BeginSession();
                                }
                            });
                        }
                        break;

                    case 204:
                        break;

                    default:
                        ob.OnError(new Exception(x.LocalizedDescription.ToString()));
                        break;
                }
            });

            session.BeginSession(); // begin initial session
            var sub = this.recordSubj.Subscribe(
                x =>
                {
                    ob.OnNext(x);
                    if (singleRead)
                    {
                        ob.OnCompleted();
                    }
                    else
                    {
                        session.InvalidateSession();

                        // begin new session
                        session = new NFCNdefReaderSession(this, DispatchQueue.CurrentQueue, true);
                        session.BeginSession();
                    }
                }
            );
            return () =>
            {
                cancel = true;
                session?.InvalidateSession();
                sub.Dispose();
                invalidSub.Dispose();
            };
        });
    }
}
