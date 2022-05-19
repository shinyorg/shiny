using System;

namespace Shiny.Nfc;


public interface INfcManager
{
    IObservable<AccessState> RequestAccess();
    IObservable<INfcTag[]> WhenTagsDetected();
}
