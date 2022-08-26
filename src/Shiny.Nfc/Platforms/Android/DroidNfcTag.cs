using System;
using System.Collections.Generic;
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


        public byte[] Identifier => this.nativeTag.GetId()!;


        public NfcTagType Type
        {
            get
            {
                if (MifareClassic.Get(this.nativeTag) != null)
                    return NfcTagType.Mifare;

                if (MifareUltralight.Get(this.nativeTag) != null)
                    return NfcTagType.Mifare;

                if (NfcA.Get(this.nativeTag) != null)
                    return NfcTagType.NfcA;

                if (NfcB.Get(this.nativeTag) != null)
                    return NfcTagType.NfcB;

                if (NfcF.Get(this.nativeTag) != null)
                    return NfcTagType.NfcF;

                if (NfcV.Get(this.nativeTag) != null)
                    return NfcTagType.NfcV;

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


        public Task<NDefRecord[]?> Read()
        {
            var ndef = Ndef.Get(this.nativeTag);
            if (ndef == null)
                return Task.FromResult<NDefRecord[]?>(null);

            //ndef.ConnectAsync();
            var records = ndef
                .NdefMessage?
                .GetRecords();

            if (records == null)
                return Task.FromResult<NDefRecord[]?>(null);

            var result = records
                .Select(record => new NDefRecord
                {
                    Identifier = record.GetId(),
                    Uri = record.ToUri()?.ToString(),
                    //MimeType => this.native.ToMimeType(); // Android only>?
                    Payload = record.GetPayload(),
                    PayloadType = record.Tnf switch
                    {
                        0x00 => NDefPayloadType.Empty,
                        0x01 => NDefPayloadType.WellKnown,
                        0x02 => NDefPayloadType.Mime,
                        0x03 => NDefPayloadType.Uri,
                        0x04 => NDefPayloadType.External,
                        0x06 => NDefPayloadType.Unchanged,
                        0x05 => NDefPayloadType.Unknown,
                        0x07 => NDefPayloadType.Unknown,
                        _ => NDefPayloadType.Unknown
                    }
                })
                .ToArray();

            return Task.FromResult<NDefRecord[]?>(result);
        }


        public async Task Write(NDefRecord[] records, bool makeReadOnly)
        {
            if (!this.IsWriteable)
                throw new InvalidOperationException("This tag is not writeable");

            var nativeRecords = new List<NdefRecord>();
            foreach (var record in records)
            {
                var native = record.PayloadType switch
                {
                    //NDefPayloadType.WellKnown => NdefRecord.CreateTextRecord()
                    NDefPayloadType.Uri => NdefRecord.CreateUri(record.Uri),
                    NDefPayloadType.External => NdefRecord.CreateExternal("domain", "type", new byte[0]),
                    NDefPayloadType.Mime => NdefRecord.CreateMime("mimeType", new byte[0]),
                    _ => throw new InvalidOperationException("Invalid payload")
                };
                nativeRecords.Add(native!);
            }

            var ndef = Ndef.Get(this.nativeTag)!;
            await ndef.ConnectAsync();
            var message = new NdefMessage(nativeRecords.ToArray());

            await ndef.WriteNdefMessageAsync(message);
            if (makeReadOnly && ndef.CanMakeReadOnly())
                await ndef.MakeReadOnlyAsync();

            ndef.Close();
        }
    }
}