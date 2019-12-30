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
        public NfcPayloadType PayloadType
        {
            get
            {
                switch (this.native.Tnf)
                {
                    case 0x00:
                        return NfcPayloadType.Empty;

					case 0x01:
                        return NfcPayloadType.WellKnown;

                    case 0x02:
                        return NfcPayloadType.Mime;

                    case 0x03:
                        return NfcPayloadType.Uri;

                    case 0x04:
                        return NfcPayloadType.External;

                    case 0x06:
                        return NfcPayloadType.Unchanged;

                    case 0x05:
                    case 0x07: // reserved?
                    default:
                        return NfcPayloadType.Unknown;
                }	
            }
        }
    }
}
