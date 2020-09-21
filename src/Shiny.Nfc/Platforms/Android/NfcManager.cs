using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android;
using Android.App;
using Android.Nfc;
using Android.Nfc.Tech;

[assembly: UsesPermission(Manifest.Permission.Nfc)]
[assembly: UsesFeature("android.permission.NFC")]


namespace Shiny.Nfc
{
    //https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
    public class NfcManager : Java.Lang.Object,
                              NfcAdapter.IReaderCallback,
                              //NfcAdapter.ICreateNdefMessageCallback,
                              //NfcAdapter.IOnNdefPushCompleteCallback,
                              INfcManager
    {
        readonly AndroidContext context;
        readonly Subject<NDefRecord[]> recordSubj;
        //readonly Subject<PushState> publishSubj;
        //Func<NDefRecord>? recordFunc;


        public NfcManager(AndroidContext context)
        {
            this.context = context;
            this.recordSubj = new Subject<NDefRecord[]>();
            //this.publishSubj = new Subject<PushState>();
        }

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

        public IObservable<AccessState> RequestAccess()
            => this.context.RequestAccess(Manifest.Permission.Nfc);


        public IObservable<NDefRecord[]> SingleRead() => this.DoRead(true);
        public IObservable<NDefRecord[]> ContinuousRead() => this.DoRead(false);


        protected virtual IObservable<NDefRecord[]> DoRead(bool singleRead) => Observable.Create<NDefRecord[]>(ob =>
        {
            var adapter = NfcAdapter.GetDefaultAdapter(this.context.AppContext);
            adapter.EnableReaderMode(
                this.context.CurrentActivity,
                this,
                NfcReaderFlags.NfcA |
                NfcReaderFlags.NfcB |
                NfcReaderFlags.NfcBarcode |
                NfcReaderFlags.NfcF |
                NfcReaderFlags.NfcV |
                NfcReaderFlags.NoPlatformSounds,
                new Android.OS.Bundle()
            );

            var sub = this.recordSubj.Subscribe(
                x =>
                {
                    ob.OnNext(x);
                    if (singleRead)
                        ob.OnCompleted();
                },
                ob.OnError
            );

            return () =>
            {
                adapter.DisableReaderMode(this.context.CurrentActivity);
                sub?.Dispose();
            };
        });


        readonly object syncLock = new object();
        public void OnTagDiscovered(Tag tag)
        {
            try
            {
                //tag.GetId();
                var ndef = Ndef.Get(tag);
                if (ndef != null)
                {
                    lock (this.syncLock)
                    {
                        if (!ndef.IsConnected)
                            ndef.Connect();
                    }
                    //ndef.Type
                    //ndef.IsConnected
                    //ndef.IsWritable
                    var records = new List<NDefRecord>();
                    var nativeRecords = ndef.NdefMessage?.GetRecords() ?? Array.Empty<NdefRecord>();

                    foreach (var record in nativeRecords)
                    {
                        records.Add(new NDefRecord
                        {
                            Identifier = record.GetId(),
                            Uri = record.ToUri()?.ToString(),
                            //MimeType => this.native.ToMimeType(); // Android only>?
                            Payload = record.GetPayload(),
                            PayloadType = record.Tnf switch
                            {
                                0x00 => NfcPayloadType.Empty,
                                0x01 => NfcPayloadType.WellKnown,
                                0x02 => NfcPayloadType.Mime,
                                0x03 => NfcPayloadType.Uri,
                                0x04 => NfcPayloadType.External,
                                0x06 => NfcPayloadType.Unchanged,
                                0x05 => NfcPayloadType.Unknown,
                                0x07 => NfcPayloadType.Unknown,
                                _ => NfcPayloadType.Unknown
                            }
                        });
                    }
                    this.recordSubj.OnNext(records.ToArray());
                }
            }
            catch (Exception ex)
            {
                this.recordSubj.OnError(ex);
            }
        }


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
