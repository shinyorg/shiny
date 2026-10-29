namespace Shiny.Nfc;


/// <summary>Something went wrong talking to a tag or the NFC hardware.</summary>
public class NfcException : Exception
{
    /// <summary>Creates the exception.</summary>
    public NfcException(string message) : base(message) { }

    /// <summary>Creates the exception.</summary>
    public NfcException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>The platform, device or tag cannot do what was asked.</summary>
/// <remarks>Check <see cref="INfcManager.Capabilities"/> first and this never fires for a platform reason.</remarks>
public class NfcNotSupportedException(string message, NfcCapabilities capability) : NfcException(message)
{
    /// <summary>The capability that was missing.</summary>
    public NfcCapabilities Capability { get; } = capability;
}


/// <summary>The tag left the field before the operation finished.</summary>
public class NfcTagLostException : NfcException
{
    /// <summary>Creates the exception.</summary>
    public NfcTagLostException() : base("The tag left the field") { }

    /// <summary>Creates the exception.</summary>
    public NfcTagLostException(Exception innerException) : base("The tag left the field", innerException) { }
}


/// <summary>The tag is locked against writing.</summary>
public class NfcTagReadOnlyException() : NfcException("The tag is read-only");


/// <summary>The bytes read from a tag are not a valid NDEF message.</summary>
public class NdefFormatException(string message) : NfcException(message);


/// <summary>The NDEF message does not fit on the tag. Nothing was written.</summary>
public class NdefMessageTooLargeException(int messageSize, int maxSize)
    : NfcException($"The NDEF message is {messageSize} bytes but the tag can hold at most {maxSize}")
{
    /// <summary>The encoded size of the message that was rejected.</summary>
    public int MessageSize { get; } = messageSize;

    /// <summary>The largest message the tag can hold.</summary>
    public int MaxSize { get; } = maxSize;
}
