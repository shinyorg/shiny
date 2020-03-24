using System;


namespace Shiny.Nfc
{
    public class NDefRecord
    {
        public byte[] Identifier { get; set; }
        public byte[]? Payload { get; set; }
        public string? Uri { get; set; }
        public NfcPayloadType PayloadType { get; set; }
    }
}
