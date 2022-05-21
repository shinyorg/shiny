using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreFoundation;
using CoreNFC;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using Shiny.Hosting;


namespace Shiny.Nfc
{
    public class NfcManager : NFCTagReaderSessionDelegate, INfcManager, IIosLifecycle.IContinueActivity
    {
        readonly ILogger logger;
        readonly Subject<INFCTag[]?> tagsSubject = new();
        public NfcManager(ILogger<NfcManager> logger) => this.logger = logger;


        //public override void DidBecomeActive(NFCTagReaderSession session) { }

        public bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler)
        {
            //            func application(_ application: UIApplication,
            //                 continue userActivity: NSUserActivity,
            //                 restorationHandler: @escaping([Any] ?) -> Void) -> Bool {

            //                guard userActivity.activityType == NSUserActivityTypeBrowsingWeb else
            //                {
            //                    return false
            //                }

            //                // Confirm that the NSUserActivity object contains a valid NDEF message.
            //                let ndefMessage = userActivity.ndefMessagePayload
            //    guard ndefMessage.records.count > 0,
            //        ndefMessage.records[0].typeNameFormat != .empty else
            //                {
            //                    return false
            //    }

            //                // Send the message to `MessagesTableViewController` for processing.
            //                guard let navigationController = window?.rootViewController as? UINavigationController else
            //                {
            //                    return false
            //                }

            //                navigationController.popToRootViewController(animated: true)
            //    let messageTableViewController = navigationController.topViewController as? MessagesTableViewController
            //    messageTableViewController?.addMessage(fromUserActivity: ndefMessage)

            //    return true

            // TODO
            return false;
        }


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
            // TODO: hot/cold observable
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                throw new InvalidOperationException("iOS 13+ is required");

            var options = NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693;
            if (AppleExtensions.HasPlistValue("com.apple.developer.nfc.readersession.felica.systemcodes"))
                options |= NFCPollingOption.Iso18092;
            //AppleExtensions.HasPlistValue("com.apple.developer.nfc.readersession.iso7816.select-identifiers");

            var session = new NFCTagReaderSession(options, this, DispatchQueue.CurrentQueue);
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
