using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreFoundation;
using CoreNFC;
using Foundation;

using UIKit;

namespace Shiny.Nfc
{
    //https://betterprogramming.pub/working-with-nfc-tags-in-ios-13-d08c7d183981
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/introduction-to-ios11/corenfc
    public class NfcManager : INfcManager, IShinyStartupTask
    {
        readonly Subject<NDefRecord[]> recordSubj = new Subject<NDefRecord[]>();
        readonly Subject<NSError> invalidSubj = new Subject<NSError>();
        readonly AppleLifecycle lifecycle;


        public NfcManager(AppleLifecycle lifecycle) => this.lifecycle = lifecycle;


        public void Start()
        {
            this.lifecycle.RegisterContinueActivity(activity =>
            {
                // TODO
                return Task.CompletedTask;
            });
        }




        public IObservable<AccessState> RequestAccess()
        {
            var status = AccessState.Available;

            if (!NFCNdefReaderSession.ReadingAvailable)
                status = AccessState.NotSupported;

            return Observable.Return(status);
        }



        public IObservable<object> Publish() => Observable.Create<object>(ob => 
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                throw new InvalidOperationException("iOS 13+ is required");

            var myDelegate = new NfcDelegates();
            var session = new NFCTagReaderSession(
                NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693 | NFCPollingOption.Iso18092, 
                myDelegate, 
                DispatchQueue.CurrentQueue
            );
            session.BeginSession();

            // TODO: detect a tag, write back to it, allow user to decide writeback at that point or force them to pass tag writeback with arg?
            return () => 
            {
                session.InvalidateSession();
                session.Dispose();
            };
        });

        public IObservable<NDefRecord[]> SingleRead() => this.DoRead(true);
        public IObservable<NDefRecord[]> ContinuousRead() => this.DoRead(false);


        protected virtual IObservable<NDefRecord[]> DoRead(bool singleRead) => Observable.Create<NDefRecord[]>(ob =>
        {
            var myDelegate = new NfcDelegates();
            var session = new NFCNdefReaderSession(myDelegate, DispatchQueue.CurrentQueue, true);
            var cancel = false;
            
            myDelegate.OnDidDetect = messages =>
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
                    ob.OnNext(list.ToArray());
                }

                if (singleRead)
                {
                    ob.OnCompleted();
                }
                else
                {
                    session.InvalidateSession();

                    // begin new session
                    session = new NFCNdefReaderSession(myDelegate, DispatchQueue.CurrentQueue, true);
                    session.BeginSession();
                }
            };

            myDelegate.OnDidInvalidate = e =>
            {
                var error = (NFCReaderError)(long)e.Code;

                switch (error)
                {
                    case NFCReaderError.ReaderSessionInvalidationErrorUserCanceled:
                        ob.OnCompleted();
                        // watch for null here
                        break;

                    case NFCReaderError.ReaderSessionInvalidationErrorSystemIsBusy:
                        if (!cancel)
                        {
                            Task.Delay(500).ContinueWith(_ =>
                            {
                                if (!cancel)
                                {
                                    session = new NFCNdefReaderSession(myDelegate, DispatchQueue.CurrentQueue, true);
                                    session.BeginSession();
                                }
                            });
                        }
                        break;

                    case NFCReaderError.ReaderSessionInvalidationErrorFirstNDEFTagRead:
                        break;

                    default:
                        ob.OnError(new Exception(e.LocalizedDescription.ToString()));
                        break;
                }
            };

            session.BeginSession(); // begin initial session

            return () =>
            {
                cancel = true;
                session?.InvalidateSession();
            };
        });        
    }
}
