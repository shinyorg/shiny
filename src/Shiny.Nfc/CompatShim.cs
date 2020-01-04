using System;
using Shiny.Nfc;


namespace Shiny
{
    public static class CrossNfc
    {
        static INfcManager Current { get; } = ShinyHost.Resolve<INfcManager>();

        public static IObservable<INDefRecord[]> Reader() => Current.Reader();
        public static IObservable<AccessState> RequestAccess(bool forBroadcasting = false) => Current.RequestAccess(forBroadcasting);
    }
}
