using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Shiny.Nfc.Pcsc;


/// <summary>
/// Linux's <c>pcsc-lite</c> (<c>libpcsclite.so.1</c>, talking to <c>pcscd</c>). Its <c>DWORD</c>
/// and <c>LONG</c> are C <c>unsigned long</c> / <c>long</c>, so every one of them is
/// <see cref="nuint"/> / <see cref="nint"/> here - 64 bits on arm64 and x64, 32 on armhf.
/// </summary>
[SupportedOSPlatform("linux")]
sealed unsafe partial class PcscLiteApi : IPcscApi
{
    const string Lib = "libpcsclite.so.1";
    const int AtrSize = 33;


    [StructLayout(LayoutKind.Sequential)]
    struct ReaderStateA
    {
        public byte* Reader;
        public void* UserData;
        public nuint CurrentState;
        public nuint EventState;
        public nuint AtrLength;
        public fixed byte Atr[AtrSize];
    }


    [StructLayout(LayoutKind.Sequential)]
    struct IoRequest
    {
        public nuint Protocol;
        public nuint PciLength;
    }


    [LibraryImport(Lib)]
    private static partial nint SCardEstablishContext(nuint scope, void* reserved1, void* reserved2, nint* context);

    [LibraryImport(Lib)]
    private static partial nint SCardReleaseContext(nint context);

    [LibraryImport(Lib)]
    private static partial nint SCardCancel(nint context);

    [LibraryImport(Lib)]
    private static partial nint SCardListReaders(nint context, byte* groups, byte* readers, nuint* length);

    [LibraryImport(Lib)]
    private static partial nint SCardGetStatusChange(nint context, nuint timeout, ReaderStateA* states, nuint count);

    [LibraryImport(Lib)]
    private static partial nint SCardConnect(nint context, byte* reader, nuint shareMode, nuint protocols, nint* card, nuint* activeProtocol);

    [LibraryImport(Lib)]
    private static partial nint SCardDisconnect(nint card, nuint disposition);

    [LibraryImport(Lib)]
    private static partial nint SCardTransmit(nint card, IoRequest* sendPci, byte* send, nuint sendLength, IoRequest* recvPci, byte* recv, nuint* recvLength);


    public nint EstablishContext()
    {
        nint context;
        try
        {
            Check("SCardEstablishContext", SCardEstablishContext(PcscConstants.ScopeUser, null, null, &context));
        }
        catch (DllNotFoundException ex)
        {
            throw new NfcNotSupportedException($"libpcsclite.so.1 is not installed - install pcscd and libpcsclite1 ({ex.Message})", NfcCapabilities.ReadNdef);
        }
        return context;
    }


    public void ReleaseContext(nint context) => SCardReleaseContext(context);
    public void Cancel(nint context) => SCardCancel(context);


    public string[] ListReaders(nint context)
    {
        nuint length = 0;
        var rc = Code(SCardListReaders(context, null, null, &length));
        if (rc == PcscError.NoReadersAvailable)
            return [];

        Check("SCardListReaders", rc);
        var buffer = new byte[(int)length];
        fixed (byte* p = buffer)
        {
            rc = Code(SCardListReaders(context, null, p, &length));
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
                rc = Code(SCardGetStatusChange(context, timeoutMs, p, (nuint)natives.Length));

            if (rc == PcscError.Success)
            {
                for (var i = 0; i < states.Length; i++)
                {
                    states[i].EventState = (uint)natives[i].EventState;
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
            nint card;
            nuint protocol;
            Check("SCardConnect", SCardConnect(
                context,
                (byte*)name,
                PcscConstants.ShareShared,
                PcscConstants.ProtocolT0 | PcscConstants.ProtocolT1,
                &card,
                &protocol
            ));
            return (card, (uint)protocol);
        }
        finally
        {
            Marshal.FreeCoTaskMem(name);
        }
    }


    public void Disconnect(nint card) => SCardDisconnect(card, PcscConstants.LeaveCard);


    public byte[] Transmit(nint card, uint protocol, ReadOnlySpan<byte> command)
    {
        var pci = new IoRequest { Protocol = protocol, PciLength = (nuint)sizeof(IoRequest) };
        var response = new byte[PcscConstants.MaxResponse];
        var length = (nuint)response.Length;

        fixed (byte* send = command)
        fixed (byte* recv = response)
            Check("SCardTransmit", SCardTransmit(card, &pci, send, (nuint)command.Length, null, recv, &length));

        return response[..(int)length];
    }


    // pcsc-lite returns LONG; its error values are positive 0x801000xx in a 64-bit long
    static uint Code(nint rc) => unchecked((uint)rc);

    static void Check(string operation, nint rc) => Check(operation, Code(rc));

    static void Check(string operation, uint rc)
    {
        if (rc != PcscError.Success)
            throw new PcscException(operation, rc);
    }
}
