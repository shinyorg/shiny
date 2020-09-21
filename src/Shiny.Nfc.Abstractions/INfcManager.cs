using System;


namespace Shiny.Nfc
{
    public interface INfcManager
    {
        // TODO: cancel all reads external to methods
        // TODO: listening status?
        //IObservable<object> Write();

        IObservable<AccessState> RequestAccess();
        IObservable<NDefRecord[]> SingleRead();
        IObservable<NDefRecord[]> ContinuousRead();

        //IObservable<object> WriteUri(string uri);
        //IObservable<object> Write(NfcPayloadType type, byte[] data);
    }
}
