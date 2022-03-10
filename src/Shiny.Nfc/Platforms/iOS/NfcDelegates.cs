using System;
using CoreNFC;
using Foundation;


namespace Shiny.Nfc
{
    public class NfcDelegates : NFCNdefReaderSessionDelegate, INFCTagReaderSessionDelegate
    {
        public Action<NFCNdefMessage[]>? OnDidDetect { get; set; }
        public Action<NSError>? OnDidInvalidate { get; set; }

        public override void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages) => this.OnDidDetect?.Invoke(messages);
        public void DidInvalidate(NFCTagReaderSession session, NSError error) => this.OnDidInvalidate?.Invoke(error);
        public override void DidInvalidate(NFCNdefReaderSession session, NSError error) => this.OnDidInvalidate?.Invoke(error);
    }
}
