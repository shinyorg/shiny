using System;
using Android.Nfc;

namespace Shiny.Nfc
{
    public class ShinyNDefRecord : INDefRecord
    {
        readonly NdefRecord native;
        public ShinyNDefRecord(NdefRecord native) => this.native = native;


		public byte[] Identifier => this.native.GetId();
        public byte[]? Payload => this.native.GetPayload();
        public string? Uri => this.native.ToUri()?.ToString();
        //public string? MimeType => this.native.ToMimeType(); // Android only>?
        public NfcPayloadType PayloadType => this.native.Tnf switch
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
        };
    }
}
