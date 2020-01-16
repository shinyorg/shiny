using System;
using Shiny.Nfc;


namespace Shiny
{
    public static class ShinyHttpTransfers
    {
        static INfcManager Current { get; } = ShinyHost.Resolve<INfcManager>();

        public static IObservable<NDefRecord[]> Reader() => Current.Reader();
        public static IObservable<AccessState> RequestAccess(bool forBroadcasting = false) => Current.RequestAccess(forBroadcasting);
    }
}
