using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreFoundation;
using CoreNFC;
using Foundation;
using UIKit;


namespace Shiny.Nfc
{
    public class NfcManager : NFCTagReaderSessionDelegate, INfcManager, IShinyStartupTask
    {
        readonly AppleLifecycle lifecycle;
        readonly Subject<INFCTag[]> tagsSubject;


        public NfcManager(AppleLifecycle lifecycle)
        {
            this.lifecycle = lifecycle;
            this.tagsSubject = new Subject<INFCTag[]>();
        }


        public void Start()
        {
            this.lifecycle.RegisterContinueActivity(activity =>
            {
                // TODO
                return Task.CompletedTask;
            });
        }


        //public override void DidBecomeActive(NFCTagReaderSession session) { }

        public override void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags) => this.tagsSubject.OnNext(tags);
        public override void DidInvalidate(NFCTagReaderSession session, NSError error)
        {
            
        }


        public IObservable<AccessState> RequestAccess()
        {
            var status = AccessState.Available;

            if (!NFCNdefReaderSession.ReadingAvailable)
                status = AccessState.NotSupported;

            return Observable.Return(status);
        }
        

        public IObservable<INfcTag[]> WhenTagsDetected() => Observable.Create<INfcTag[]>(ob =>
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                throw new InvalidOperationException("iOS 13+ is required");

            var session = new NFCTagReaderSession(
                NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693 | NFCPollingOption.Iso18092,
                this,
                DispatchQueue.CurrentQueue
            );
            // TODO: alert message
            session.BeginSession();

            var sub = this.tagsSubject.Subscribe(tags =>
            {
                //ob.OnNext(new IosNfcTag(session, tag));
            });

            return () =>
            {
                session.InvalidateSession();
                session.Dispose();
                sub.Dispose();
            };
        });
    }
}
