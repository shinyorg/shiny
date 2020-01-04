using System;
using System.Threading.Tasks;

namespace Shiny.Nfc
{
    public interface INfcManager
    {
        IObservable<AccessState> RequestAccess(bool forBroadcasting = false);
        //IObservable<object> Broadcast();
        IObservable<INDefRecord[]> Reader();
    }
}
