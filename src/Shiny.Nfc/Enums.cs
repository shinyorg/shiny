namespace Shiny.Nfc;

public enum PushState
{
    Started,
    Completed
}


public enum NDefPayloadType
{
    Unknown,
    Empty,
    Uri,
    Mime,
    WellKnown,
    External,
    Unchanged
}


public enum NfcTagType
{
    Unknown,

    // both
    Mifare,

    // ios only
    FeliCa,
    Iso7816,
    Iso15693,

    // droid only
    NfcA,
    NfcB,
    NfcF,
    NfcV
}