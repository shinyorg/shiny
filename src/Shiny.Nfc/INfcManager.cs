using System;


namespace Shiny.Nfc
{
    public interface INfcManager
    {
        IObservable<AccessState> RequestAccess();
        IObservable<NDefRecord[]> SingleRead();
        IObservable<NDefRecord[]> ContinuousRead();

        //IObservable<object> WriteUri(string uri);
        //IObservable<object> Write(NfcPayloadType type, byte[] data);
    }
}
