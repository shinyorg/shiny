using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Nfc;
using Android.Nfc.Tech;


namespace Shiny.Nfc
{
    public class DroidNfcTag : INfcTag
    {
        readonly Tag nativeTag;
        public DroidNfcTag(Tag nativeTag) => this.nativeTag = nativeTag;


        public NfcTagType Type
        {
            get
            {
                if (MifareClassic.Get(this.nativeTag) != null)
                    return NfcTagType.Mifare;

                if (MifareUltralight.Get(this.nativeTag) != null)
                    return NfcTagType.Mifare;

                //NfcA.Get(tag);
                //NfcB.Get(tag);
                //NfcF.Get(tag);
                //NfcV.Get(tag);
                //NdefFormatable.Get(tag);
                return NfcTagType.Unknown;
            }
        }

        public bool IsWriteable
        {
            get
            {
                var ndef = Ndef.Get(this.nativeTag);
                return ndef?.IsWritable ?? false;
            }
        }


        public Task<NDefRecord[]> Read()
        {
            var ndef = Ndef.Get(this.nativeTag);

            //ndef.ConnectAsync();
            //ndef.CanMakeReadOnly
            var result = ndef
                .NdefMessage?
                .GetRecords()
                .Select(x => new NDefRecord
                {
                    //            Identifier = record.GetId(),
                    //            Uri = record.ToUri()?.ToString(),
                    //            //MimeType => this.native.ToMimeType(); // Android only>?
                    //            Payload = record.GetPayload(),
                    //            PayloadType = record.Tnf switch
                    //            {
                    //                0x00 => NfcPayloadType.Empty,
                    //                0x01 => NfcPayloadType.WellKnown,
                    //                0x02 => NfcPayloadType.Mime,
                    //                0x03 => NfcPayloadType.Uri,
                    //                0x04 => NfcPayloadType.External,
                    //                0x06 => NfcPayloadType.Unchanged,
                    //                0x05 => NfcPayloadType.Unknown,
                    //                0x07 => NfcPayloadType.Unknown,
                    //                _ => NfcPayloadType.Unknown
                    //            }
                })
                .ToArray();

            return Task.FromResult(result);
        }


        public async Task Write(NDefRecord[] records, bool makeReadOnly)
        {
            var ndef = Ndef.Get(this.nativeTag);

            await ndef.ConnectAsync();
            // TODO: build message
            //ndef.WriteNdefMessageAsync(new NdefMessage())
            //ndef.CanMakeReadOnly()
            ndef.Close();
        }
    }
}