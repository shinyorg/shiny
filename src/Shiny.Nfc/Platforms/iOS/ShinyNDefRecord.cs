using System;
using CoreNFC;


namespace Shiny.Nfc
{
    public class ShinyNDefRecord : INDefRecord
    {
        readonly NFCNdefPayload native;
        public ShinyNDefRecord(NFCNdefPayload native) => this.native = native;


        public byte[] Identifier => this.native.Identifier.ToArray();
        public byte[]? Payload => this.native.Payload?.ToArray();
        public string? Uri => this.native.WellKnownTypeUriPayload?.ToString();
        public NfcPayloadType PayloadType
        {
            get
            {
                switch (this.native.TypeNameFormat)
                {
                    case NFCTypeNameFormat.AbsoluteUri:
                        return NfcPayloadType.Uri;

                    case NFCTypeNameFormat.Empty:
                        return NfcPayloadType.Empty;

                    //case NFCTypeNameFormat.Media: // TODO: mime?

                    case NFCTypeNameFormat.NFCExternal:
                        return NfcPayloadType.External;

                    case NFCTypeNameFormat.NFCWellKnown:
                        return NfcPayloadType.WellKnown;

                    case NFCTypeNameFormat.Unchanged:
                        return NfcPayloadType.Unchanged;

                    default:
                        return NfcPayloadType.Unknown;
                }
            }
        }
    }
}
