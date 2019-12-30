using System;


namespace Shiny.Nfc
{
    public interface INDefRecord
    {
        byte[] Identifier { get; }
        byte[]? Payload { get; }
        string? Uri { get; }
        NfcPayloadType PayloadType { get; }
    }
}
