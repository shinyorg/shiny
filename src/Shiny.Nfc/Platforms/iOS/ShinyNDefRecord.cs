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
        public NfcPayloadType PayloadType => this.native.TypeNameFormat switch
        {
            NFCTypeNameFormat.AbsoluteUri => NfcPayloadType.Uri,
            NFCTypeNameFormat.Empty => NfcPayloadType.Empty,
            NFCTypeNameFormat.NFCExternal => NfcPayloadType.External,
            NFCTypeNameFormat.NFCWellKnown => NfcPayloadType.WellKnown,
            NFCTypeNameFormat.Unchanged => NfcPayloadType.Unchanged,
            //case NFCTypeNameFormat.Media: // TODO: mime?
            _ => NfcPayloadType.Unknown
        };
    }
}
