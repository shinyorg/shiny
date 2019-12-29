using System;
using System.Threading.Tasks;
using CoreFoundation;
using CoreNFC;


namespace Shiny.Nfc
{
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/introduction-to-ios11/corenfc
    public class NfcManager : INfcManager
    {
        NFCNdefReaderSession? session;


        public bool IsListening => this.session != null;


        public Task<AccessState> RequestAccess(bool forWrite = false)
        {
            var status = AccessState.Available;
            if (forWrite)
                status = AccessState.NotSupported;

            else if (!NFCNdefReaderSession.ReadingAvailable)
                status = AccessState.Unknown;

            return Task.FromResult(status);
        }


        public void StartListener()
        {
            var native = new ShinyNfcDelegate();
            this.session = new NFCNdefReaderSession(native, DispatchQueue.CurrentQueue, true);
            this.session?.BeginSession();
        }


        public void StopListening() => this.session?.InvalidateSession();
    }
}
