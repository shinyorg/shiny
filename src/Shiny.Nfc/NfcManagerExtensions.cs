using System;
using System.Reactive.Linq;

namespace Shiny.Nfc;


public static class NfcManagerExtensions
{
    /// <summary>
    /// Transforms a tag detection to also pull available NDEF records from tag
    /// You should catch errors as reads can fail and shutdown this observable
    /// </summary>
    /// <param name="nfcManager"></param>
    /// <returns></returns>
    public static IObservable<(INfcTag Tag, NDefRecord[]? Records)> WhenRecordsDetected(this INfcManager nfcManager) => nfcManager
        .WhenTagsDetected()
        .SelectMany(x => x.ToObservable())
        .Select(x => Observable.FromAsync(async () =>
        {
            var records = await x.Read();
            return (x, records);
        }))
        .Switch();


    /// <summary>
    /// This will write NDEF records to any writeable NFC tag that is detected.  When a tag is written, it is broadcast out to log
    /// You should catch errors as writes can fail and shutdown this observable
    /// </summary>
    /// <param name="nfcManager"></param>
    /// <param name="makeReadonly"></param>
    /// <param name="records"></param>
    /// <returns></returns>
    public static IObservable<INfcTag> Broadcast(this INfcManager nfcManager, bool makeReadonly, params NDefRecord[] records) => nfcManager
        .WhenTagsDetected()
        .SelectMany(x => x.ToObservable())
        .Where(x => x.IsWriteable)
        .Select(x => Observable.FromAsync(async () =>
        {
            await x.Write(records, makeReadonly);
            return x;
        }))
        .Switch();
}
