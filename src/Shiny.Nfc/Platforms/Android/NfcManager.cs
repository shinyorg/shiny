using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android;
using Android.Nfc;


namespace Shiny.Nfc
{
    //https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
    public class NfcManager : Java.Lang.Object, NfcAdapter.IReaderCallback, INfcManager
    {
        readonly AndroidContext context;
        readonly Subject<INDefRecord[]> recordSubj;
        

        public NfcManager(AndroidContext context)
        {
            this.context = context;
            this.recordSubj = new Subject<INDefRecord[]>();
        }


        public IObservable<AccessState> RequestAccess(bool forPublishing = false)
            => this.context.RequestAccess(Manifest.Permission.Nfc);


        public IObservable<INDefRecord[]> Reader() => Observable.Create<INDefRecord[]>(ob =>
        {
            var adapter = NfcAdapter.GetDefaultAdapter(this.context.AppContext);

            adapter.EnableReaderMode(
                this.context.CurrentActivity,
                this,
                NfcReaderFlags.NfcA,
                new Android.OS.Bundle()
            );
            var sub = this.recordSubj.Subscribe(ob.OnNext);

            return () =>
            {
                adapter.DisableReaderMode(this.context.CurrentActivity);
                sub.Dispose();
            };
        });


        public void OnTagDiscovered(Tag tag)
        {
        }
    }
}
