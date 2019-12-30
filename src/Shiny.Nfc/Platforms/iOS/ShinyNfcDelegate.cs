using System;
using System.Collections.Generic;
using CoreNFC;
using Foundation;
using Shiny.Logging;


namespace Shiny.Nfc
{
    public class ShinyNfcDelegate : NFCNdefReaderSessionDelegate
    {
        readonly Lazy<INfcDelegate> sdelegate = new Lazy<INfcDelegate>(() => ShinyHost.Resolve<INfcDelegate>());


        public override void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages) => Dispatcher.ExecuteBackgroundTask(async () =>
        {
            foreach (var message in messages)
            {
                var list = new List<ShinyNDefRecord>(message.Records.Length);
                foreach (var record in message.Records)
                    list.Add(new ShinyNDefRecord(record));

                await Log.SafeExecute(() => this.sdelegate.Value.OnReceived(list.ToArray()));
            }
        });


        public override void DidInvalidate(NFCNdefReaderSession session, NSError error)
        {
            // TODO: wire up error to delegate?  Log it?
        }
    }
}
