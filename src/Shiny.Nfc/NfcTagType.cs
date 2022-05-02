namespace Shiny.Nfc
{ 
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
}