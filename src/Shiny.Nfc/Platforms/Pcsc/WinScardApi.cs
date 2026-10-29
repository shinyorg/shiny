using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Shiny.Nfc.Pcsc;


/// <summary>Windows' <c>winscard.dll</c>: pointer-sized handles, 32-bit DWORDs, UTF-16 names, 36-byte ATR.</summary>
[SupportedOSPlatform("windows")]
sealed unsafe partial class WinScardApi : IPcscApi
{
    const string Lib = "winscard.dll";
    const int AtrSize = 36;


    [StructLayout(LayoutKind.Sequential)]
    struct ReaderStateW
    {
        public char* Reader;
        public void* UserData;
        public uint CurrentState;
        public uint EventState;
        public uint AtrLength;
        public fixed byte Atr[AtrSize];
    }


    [StructLayout(LayoutKind.Sequential)]
    struct IoRequest
    {
        public uint Protocol;
        public uint PciLength;
    }


    [LibraryImport(Lib)]
    private static partial int SCardEstablishContext(uint scope, void* reserved1, void* reserved2, nint* context);

    [LibraryImport(Lib)]
    private static partial int SCardReleaseContext(nint context);

    [LibraryImport(Lib)]
    private static partial int SCardCancel(nint context);

    [LibraryImport(Lib)]
    private static partial int SCardListReadersW(nint context, char* groups, char* readers, uint* length);

    [LibraryImport(Lib)]
    private static partial int SCardGetStatusChangeW(nint context, uint timeout, ReaderStateW* states, uint count);

    [LibraryImport(Lib)]
    private static partial int SCardConnectW(nint context, char* reader, uint shareMode, uint protocols, nint* card, uint* activeProtocol);

    [LibraryImport(Lib)]
    private static partial int SCardDisconnect(nint card, uint disposition);

    [LibraryImport(Lib)]
    private static partial int SCardTransmit(nint card, IoRequest* sendPci, byte* send, uint sendLength, IoRequest* recvPci, byte* recv, uint* recvLength);


    public nint EstablishContext()
    {
        nint context;
        Check("SCardEstablishContext", SCardEstablishContext(PcscConstants.ScopeUser, null, null, &context));
        return context;
    }


    public void ReleaseContext(nint context) => SCardReleaseContext(context);
    public void Cancel(nint context) => SCardCancel(context);


    public string[] ListReaders(nint context)
    {
        uint length = 0;
        var rc = (uint)SCardListReadersW(context, null, null, &length);
        if (rc == PcscError.NoReadersAvailable)
            return [];

        Check("SCardListReaders", rc);
        var buffer = new char[length];
        fixed (char* p = buffer)
        {
            rc = (uint)SCardListReadersW(context, null, p, &length);
            if (rc == PcscError.NoReadersAvailable)
                return [];

            Check("SCardListReaders", rc);
        }
        return new string(buffer, 0, (int)length).Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }


    public uint GetStatusChange(nint context, ReaderState[] states, uint timeoutMs)
    {
        var natives = new ReaderStateW[states.Length];
        var names = new nint[states.Length];
        try
        {
            for (var i = 0; i < states.Length; i++)
            {
                names[i] = Marshal.StringToHGlobalUni(states[i].Reader);
                natives[i].Reader = (char*)names[i];
                natives[i].CurrentState = states[i].CurrentState;
            }

            uint rc;
            fixed (ReaderStateW* p = natives)
                rc = (uint)SCardGetStatusChangeW(context, timeoutMs, p, (uint)natives.Length);

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
                Marshal.FreeHGlobal(name);
        }
    }


    public (nint Card, uint Protocol) Connect(nint context, string reader)
    {
        nint card;
        uint protocol;
        fixed (char* name = reader)
        {
            Check("SCardConnect", SCardConnectW(
                context,
                name,
                PcscConstants.ShareShared,
                PcscConstants.ProtocolT0 | PcscConstants.ProtocolT1,
                &card,
                &protocol
            ));
        }
        return (card, protocol);
    }


    public void Disconnect(nint card) => SCardDisconnect(card, PcscConstants.LeaveCard);


    public byte[] Transmit(nint card, uint protocol, ReadOnlySpan<byte> command)
    {
        var pci = new IoRequest { Protocol = protocol, PciLength = (uint)sizeof(IoRequest) };
        var response = new byte[PcscConstants.MaxResponse];
        var length = (uint)response.Length;

        fixed (byte* send = command)
        fixed (byte* recv = response)
            Check("SCardTransmit", SCardTransmit(card, &pci, send, (uint)command.Length, null, recv, &length));

        return response[..(int)length];
    }


    static void Check(string operation, int rc) => Check(operation, (uint)rc);

    static void Check(string operation, uint rc)
    {
        if (rc != PcscError.Success)
            throw new PcscException(operation, rc);
    }
}
