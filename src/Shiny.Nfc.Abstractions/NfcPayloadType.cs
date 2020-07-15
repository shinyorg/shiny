using System;


namespace Shiny.Nfc
{
    public enum NfcPayloadType
    {
        Unknown,
        Empty,
        Uri,
        Mime,
        WellKnown,
        External,
        Unchanged
    }
}
