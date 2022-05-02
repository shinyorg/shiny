//using System;
//using System.Reactive.Linq;


//namespace Shiny.Nfc
//{
//    public static class NfcManagerExtensions
//    {
//        public static IObservable<NDefRecord[]> WhenRecordsDetected(this INfcManager manager) => manager
//            .WhenTagsDetected()
//            .SelectMany(x => Observable.FromAsync(() => x.));
//    }
//}
