using System;
using System.Reactive.Linq;
using Android;
using Android.Nfc;


namespace Shiny.Nfc
{
    //https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
    public class NfcManager : Java.Lang.Object,
                              //NfcAdapter.IReaderCallback,
                              //NfcAdapter.ICreateNdefMessageCallback,
                              //NfcAdapter.IOnNdefPushCompleteCallback,
                              INfcManager
    {
        readonly IPlatform platform;


        public NfcManager(IPlatform platform)
        {
            this.platform = platform;
            this.platform
                .WhenIntentReceived()
                // TODO: others
                .Where(x => x.Action?.Equals(NfcAdapter.ActionNdefDiscovered, StringComparison.InvariantCultureIgnoreCase) ?? false)
                .Subscribe(x =>
                {

                });
        }



        public IObservable<AccessState> RequestAccess()
            => this.platform.RequestAccess(Manifest.Permission.Nfc);


        public IObservable<INfcTag[]> WhenTagsDetected() => throw new NotImplementedException();

        //public IObservable<PushState> Publish(NDefRecord record) => this.Publish(() => record);
        //public IObservable<PushState> Publish(Func<NDefRecord> pushFunc) => Observable.Create<PushState>(ob =>
        //{
        //    this.recordFunc = pushFunc;
        //    var adapter = NfcAdapter.GetDefaultAdapter(this.context.AppContext);
        //    adapter.SetNdefPushMessage(null, this.context.CurrentActivity);
        //    adapter.SetNdefPushMessageCallback(this, this.context.CurrentActivity);
        //    adapter.SetOnNdefPushCompleteCallback(this, this.context.CurrentActivity);
        //    return this.publishSubj.Subscribe(ob.OnNext);
        //});




        //protected virtual IObservable<NDefRecord[]> DoRead(bool singleRead) => Observable.Create<NDefRecord[]>(async ob =>
        //{
        //    IDisposable? sub = null;
        //    var adapter = NfcAdapter.GetDefaultAdapter(this.platform.AppContext);
        //    var access = await this.RequestAccess();

        //    if (access != AccessState.Available)
        //    {
        //        ob.OnError(new PermissionException("NFC", access));
        //    }
        //    else
        //    {
        //        adapter.EnableReaderMode(
        //            this.platform.CurrentActivity,
        //            this,
        //            NfcReaderFlags.NfcA |
        //            NfcReaderFlags.NfcB |
        //            NfcReaderFlags.NfcBarcode |
        //            NfcReaderFlags.NfcF |
        //            NfcReaderFlags.NfcV |
        //            NfcReaderFlags.NoPlatformSounds,
        //            new Android.OS.Bundle()
        //        );

        //        sub = this.recordSubj.Subscribe(
        //            x =>
        //            {
        //                ob.OnNext(x);
        //                if (singleRead)
        //                    ob.OnCompleted();
        //            },
        //            ob.OnError
        //        );
        //    }

        //    return () =>
        //    {
        //        adapter.DisableReaderMode(this.platform.CurrentActivity);
        //        sub?.Dispose();
        //    };
        //});


        //public void OnTagDiscovered(Tag tag)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        this.recordSubj.OnError(ex);
        //    }
        //}

        //public NdefMessage CreateNdefMessage(NfcEvent e)
        //{
        //    this.publishSubj.OnNext(PushState.Started);
        //    var record = this.recordFunc.Invoke();
        //    //NdefRecord.CreateExternal
        //    //NdefRecord.CreateExternal()
        //    //NdefRecord.CreateMime()
        //    //NdefRecord.CreateTextRecord()
        //    //NdefRecord.CreateUri()
        //    //return new NdefMessage(new NdefRecord());
        //    return null;
        //}


        //public void OnNdefPushComplete(NfcEvent e)
        //    => this.publishSubj.OnNext(PushState.Completed);
    }
}
