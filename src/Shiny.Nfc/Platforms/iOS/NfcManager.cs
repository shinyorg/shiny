using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreFoundation;
using CoreNFC;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;


namespace Shiny.Nfc
{
    public class NfcManager : NFCTagReaderSessionDelegate, INfcManager, IShinyStartupTask
    {
        readonly AppleLifecycle lifecycle;
        readonly ILogger logger;
        readonly Subject<INFCTag[]?> tagsSubject;
        

        public NfcManager(AppleLifecycle lifecycle, ILogger<NfcManager> logger)
        {
            this.lifecycle = lifecycle;
            this.logger = logger;
            this.tagsSubject = new Subject<INFCTag[]?>();
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

        public override void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags) 
            => this.tagsSubject.OnNext(tags);


        public override void DidInvalidate(NFCTagReaderSession session, NSError error)
        {
            if (error == null)
            {
                this.logger.LogDebug("NFC session has invalided");
            }
            else
            {
                var ex = new Exception("NFC session has invalidated due to an error: " + error.LocalizedDescription);
                this.logger.LogError(ex, "NFC session has invalidated");
                this.tagsSubject.OnError(ex);
            }
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

            var sub = this.tagsSubject.Materialize().Subscribe(notification =>
            {
                if (notification.Exception != null)
                {
                    ob.OnError(notification.Exception);
                }
                else
                { 
                    var shinyTags = notification.Value?.Select(tag => new IosNfcTag(session, tag)).ToArray() ?? Array.Empty<INfcTag>();
                    ob.OnNext(shinyTags);
                }
            });

            return () =>
            {
                sub.Dispose();
                session.InvalidateSession();
                session.Dispose();
            };
        });
    }
}
