using System;



namespace Shiny.Nfc
{
    public class NfcManager : INfcManager
    {
        public IObservable<AccessState> RequestAccess() => null;
        public IObservable<NDefRecord[]> SingleRead() => null;
        public IObservable<NDefRecord[]> ContinuousRead() => null;
    }
}
