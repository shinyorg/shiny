using System;


namespace Shiny.Nfc
{
    public interface INfcManager
    {
        IObservable<AccessState> RequestAccess();
        IObservable<INfcTag[]> WhenTagsDetected();

        //IObservable<object> Publish(NDefRecord record);
        // TODO: observable for "background" receive or delegate?
        //IObservable<object> WriteUri(string uri);
        //IObservable<object> Write(NfcPayloadType type, byte[] data);
    }
}
