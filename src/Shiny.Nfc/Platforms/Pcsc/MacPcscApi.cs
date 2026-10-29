using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Shiny.Nfc.Pcsc;


/// <summary>
/// macOS' <c>PCSC.framework</c>: 32-bit handles and DWORDs, UTF-8 names, a 33-byte ATR, and a
/// reader state declared under <c>#pragma pack(1)</c> - no padding after the pointers or the ATR.
/// </summary>
[SupportedOSPlatform("macos")]
sealed unsafe partial class MacPcscApi : IPcscApi
{
    const string Lib = "/System/Library/Frameworks/PCSC.framework/PCSC";
    const int AtrSize = 33;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ReaderStateA
    {
        public byte* Reader;
        public void* UserData;
        public uint CurrentState;
        public uint EventState;
        public uint AtrLength;
        public fixed byte Atr[AtrSize];
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IoRequest
    {
        public uint Protocol;
        public uint PciLength;
    }


    [LibraryImport(Lib)]
    private static partial int SCardEstablishContext(uint scope, void* reserved1, void* reserved2, int* context);

    [LibraryImport(Lib)]
    private static partial int SCardReleaseContext(int context);

    [LibraryImport(Lib)]
    private static partial int SCardCancel(int context);

    [LibraryImport(Lib)]
    private static partial int SCardListReaders(int context, byte* groups, byte* readers, uint* length);

    [LibraryImport(Lib)]
    private static partial int SCardGetStatusChange(int context, uint timeout, ReaderStateA* states, uint count);

    [LibraryImport(Lib)]
    private static partial int SCardConnect(int context, byte* reader, uint shareMode, uint protocols, int* card, uint* activeProtocol);

    [LibraryImport(Lib)]
    private static partial int SCardDisconnect(int card, uint disposition);

    [LibraryImport(Lib)]
    private static partial int SCardTransmit(int card, IoRequest* sendPci, byte* send, uint sendLength, IoRequest* recvPci, byte* recv, uint* recvLength);


    public nint EstablishContext()
    {
        int context;
        Check("SCardEstablishContext", SCardEstablishContext(PcscConstants.ScopeUser, null, null, &context));
        return context;
    }


    public void ReleaseContext(nint context) => SCardReleaseContext((int)context);
    public void Cancel(nint context) => SCardCancel((int)context);


    public string[] ListReaders(nint context)
    {
        uint length = 0;
        var rc = (uint)SCardListReaders((int)context, null, null, &length);
        if (rc == PcscError.NoReadersAvailable)
            return [];

        Check("SCardListReaders", rc);
        var buffer = new byte[length];
        fixed (byte* p = buffer)
        {
            rc = (uint)SCardListReaders((int)context, null, p, &length);
            if (rc == PcscError.NoReadersAvailable)
                return [];

            Check("SCardListReaders", rc);
        }
        return Encoding.UTF8.GetString(buffer, 0, (int)length).Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }


    public uint GetStatusChange(nint context, ReaderState[] states, uint timeoutMs)
    {
        var natives = new ReaderStateA[states.Length];
        var names = new nint[states.Length];
        try
        {
            for (var i = 0; i < states.Length; i++)
            {
                names[i] = Marshal.StringToCoTaskMemUTF8(states[i].Reader);
                natives[i].Reader = (byte*)names[i];
                natives[i].CurrentState = states[i].CurrentState;
            }

            uint rc;
            fixed (ReaderStateA* p = natives)
                rc = (uint)SCardGetStatusChange((int)context, timeoutMs, p, (uint)natives.Length);

            if (rc == PcscError.Success)
            {
                for (var i = 0; i < states.Length; i++)
                {
                    states[i].EventState = natives[i].EventState;
                    var length = Math.Min((int)natives[i].AtrLength, AtrSize);
                    fixed (byte* atr = natives[i].Atr)
                        states[i].Atr = new ReadOnlySpan<byte>(atr, length).ToArray();
                }
            }
            return rc;
        }
        finally
        {
            foreach (var name in names)
                Marshal.FreeCoTaskMem(name);
        }
    }


    public (nint Card, uint Protocol) Connect(nint context, string reader)
    {
        var name = Marshal.StringToCoTaskMemUTF8(reader);
        try
        {
            int card;
            uint protocol;
            Check("SCardConnect", SCardConnect(
                (int)context,
                (byte*)name,
                PcscConstants.ShareShared,
                PcscConstants.ProtocolT0 | PcscConstants.ProtocolT1,
                &card,
                &protocol
            ));
            return (card, protocol);
        }
        finally
        {
            Marshal.FreeCoTaskMem(name);
        }
    }


    public void Disconnect(nint card) => SCardDisconnect((int)card, PcscConstants.LeaveCard);


    public byte[] Transmit(nint card, uint protocol, ReadOnlySpan<byte> command)
    {
        var pci = new IoRequest { Protocol = protocol, PciLength = (uint)sizeof(IoRequest) };
        var response = new byte[PcscConstants.MaxResponse];
        var length = (uint)response.Length;

        fixed (byte* send = command)
        fixed (byte* recv = response)
            Check("SCardTransmit", SCardTransmit((int)card, &pci, send, (uint)command.Length, null, recv, &length));

        return response[..(int)length];
    }


    static void Check(string operation, int rc) => Check(operation, (uint)rc);

    static void Check(string operation, uint rc)
    {
        if (rc != PcscError.Success)
            throw new PcscException(operation, rc);
    }
}
