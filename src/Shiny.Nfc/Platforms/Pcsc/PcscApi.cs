namespace Shiny.Nfc.Pcsc;


/// <summary>
/// The handful of PC/SC calls the scan needs, with every platform's ABI differences hidden.
/// </summary>
/// <remarks>
/// <para>The three implementations look the same and are not: Windows' <c>winscard.dll</c> has
/// pointer-sized handles, 32-bit <c>DWORD</c>s and a 36-byte ATR; macOS' <c>PCSC.framework</c> has
/// 32-bit handles and a <c>#pragma pack(1)</c> reader state; Linux's <c>pcsc-lite</c> makes
/// <c>DWORD</c> an <c>unsigned long</c> - 64 bits on arm64 and x64. Sharing one struct between them
/// corrupts memory without failing to compile.</para>
/// <para>Calls block; the caller keeps them off threads that matter.</para>
/// </remarks>
interface IPcscApi
{
    /// <summary>Returns the context, or throws <see cref="PcscException"/>.</summary>
    nint EstablishContext();
    void ReleaseContext(nint context);

    /// <summary>Makes a blocked <see cref="GetStatusChange"/> on this context return <see cref="PcscError.Cancelled"/>.</summary>
    void Cancel(nint context);

    /// <summary>The connected readers - empty, not an error, when there are none.</summary>
    string[] ListReaders(nint context);

    /// <summary>
    /// Waits up to <paramref name="timeoutMs"/> for any reader's state to differ from its
    /// <see cref="ReaderState.CurrentState"/>, then fills in <see cref="ReaderState.EventState"/>
    /// and <see cref="ReaderState.Atr"/>. Returns the PC/SC result code.
    /// </summary>
    uint GetStatusChange(nint context, ReaderState[] states, uint timeoutMs);

    /// <summary>Connects to the card in <paramref name="reader"/>, or throws <see cref="PcscException"/>.</summary>
    (nint Card, uint Protocol) Connect(nint context, string reader);
    void Disconnect(nint card);

    /// <summary>Sends one APDU and returns the response, or throws <see cref="PcscException"/>.</summary>
    byte[] Transmit(nint card, uint protocol, ReadOnlySpan<byte> command);
}


/// <summary>One reader's entry in a <c>SCardGetStatusChange</c> call.</summary>
sealed class ReaderState(string reader, uint currentState)
{
    public string Reader { get; } = reader;
    public uint CurrentState { get; set; } = currentState;
    public uint EventState { get; set; }
    public byte[] Atr { get; set; } = [];
}


static class PcscState
{
    public const uint Unaware = 0x0000;
    public const uint Changed = 0x0002;
    public const uint Unavailable = 0x0008;
    public const uint Empty = 0x0010;
    public const uint Present = 0x0020;
    public const uint Mute = 0x0200;
}


static class PcscConstants
{
    public const uint ScopeUser = 0;
    public const uint ShareShared = 2;
    public const uint ProtocolT0 = 1;
    public const uint ProtocolT1 = 2;
    public const uint LeaveCard = 0;

    /// <summary>The largest response a short APDU can carry: 256 data bytes and the status word.</summary>
    public const int MaxResponse = 258;
}


/// <summary>PC/SC result codes, normalised to their unsigned 32-bit values on every platform.</summary>
static class PcscError
{
    public const uint Success = 0;
    public const uint Cancelled = 0x80100002;
    public const uint InvalidHandle = 0x80100003;
    public const uint UnknownReader = 0x80100009;
    public const uint Timeout = 0x8010000A;
    public const uint NoSmartcard = 0x8010000C;
    public const uint ReaderUnavailable = 0x80100017;
    public const uint NoService = 0x8010001D;
    public const uint ServiceStopped = 0x8010001E;
    public const uint NoReadersAvailable = 0x8010002E;
    public const uint UnresponsiveCard = 0x80100066;
    public const uint UnpoweredCard = 0x80100067;
    public const uint ResetCard = 0x80100068;
    public const uint RemovedCard = 0x80100069;

    /// <summary>The card went away, or its handle is gone with it.</summary>
    public static bool IsCardGone(uint code) => code is RemovedCard or ResetCard or NoSmartcard or InvalidHandle or UnpoweredCard or UnresponsiveCard;

    /// <summary>The reader went away - unplugged mid-call.</summary>
    public static bool IsReaderGone(uint code) => code is ReaderUnavailable or UnknownReader;
}


/// <summary>A PC/SC call failed.</summary>
sealed class PcscException(string operation, uint code) : Exception($"{operation} failed with PC/SC error 0x{code:X8}")
{
    public uint Code { get; } = code;
}
