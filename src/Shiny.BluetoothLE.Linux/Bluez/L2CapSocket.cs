using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Bluez;


/// <summary>
/// Thin wrapper around the Linux AF_BLUETOOTH / BTPROTO_L2CAP socket interface used to open
/// LE L2CAP Connection-Oriented Channels (CoC). BlueZ does not expose CoC over D-Bus, so we
/// drop down to the kernel socket API directly.
/// </summary>
internal static partial class L2CapSocket
{
    // <sys/socket.h> / <bluetooth/bluetooth.h>
    const int AF_BLUETOOTH = 31;
    const int SOCK_SEQPACKET = 5;
    const int BTPROTO_L2CAP = 0;

    const int SOL_BLUETOOTH = 274;
    const int BT_SECURITY = 4;

    public const byte BT_SECURITY_LOW = 1;
    public const byte BT_SECURITY_MEDIUM = 2;
    public const byte BT_SECURITY_HIGH = 3;

    public const byte BDADDR_LE_PUBLIC = 1;
    public const byte BDADDR_LE_RANDOM = 2;

    // Default MTU for LE CoC is 23, but with negotiation can grow to 65535. 4096 covers the common case.
    const int ReadBufferSize = 4096;


    [LibraryImport("libc", EntryPoint = "socket", SetLastError = true)]
    private static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc", EntryPoint = "bind", SetLastError = true)]
    private static partial int bind(int sockfd, ref SockAddrL2 addr, int addrlen);

    [LibraryImport("libc", EntryPoint = "connect", SetLastError = true)]
    private static partial int connect(int sockfd, ref SockAddrL2 addr, int addrlen);

    [LibraryImport("libc", EntryPoint = "listen", SetLastError = true)]
    private static partial int listen(int sockfd, int backlog);

    [LibraryImport("libc", EntryPoint = "accept", SetLastError = true)]
    private static partial int accept(int sockfd, ref SockAddrL2 addr, ref int addrlen);

    [LibraryImport("libc", EntryPoint = "getsockname", SetLastError = true)]
    private static partial int getsockname(int sockfd, ref SockAddrL2 addr, ref int addrlen);

    [LibraryImport("libc", EntryPoint = "setsockopt", SetLastError = true)]
    private static partial int setsockopt(int sockfd, int level, int optname, ref BtSecurity optval, int optlen);

    [LibraryImport("libc", EntryPoint = "shutdown", SetLastError = true)]
    private static partial int shutdown(int sockfd, int how);

    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    private static partial int close(int fd);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    private static partial nint read(int fd, IntPtr buf, nuint count);

    [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
    private static partial nint write(int fd, IntPtr buf, nuint count);


    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    private struct SockAddrL2
    {
        public ushort l2_family;
        public ushort l2_psm;          // little-endian on the wire
        public byte b0, b1, b2, b3, b4, b5;
        public ushort l2_cid;          // little-endian
        public byte l2_bdaddr_type;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BtSecurity
    {
        public byte level;
        public byte key_size;
    }


    /// <summary>
    /// Opens a connected L2CAP CoC to the supplied peer. Throws <see cref="IOException"/> on failure.
    /// </summary>
    /// <param name="bdAddr">Peer device address in "AA:BB:CC:DD:EE:FF" form (BlueZ Device1.Address).</param>
    /// <param name="addressType">BDADDR_LE_PUBLIC or BDADDR_LE_RANDOM.</param>
    /// <param name="psm">PSM advertised by the peripheral.</param>
    /// <param name="secure">True requests BT_SECURITY_MEDIUM, false BT_SECURITY_LOW.</param>
    public static L2CapHandle Connect(string bdAddr, byte addressType, ushort psm, bool secure)
    {
        var fd = socket(AF_BLUETOOTH, SOCK_SEQPACKET, BTPROTO_L2CAP);
        ThrowIfNegative(fd, "socket");

        try
        {
            SetSecurity(fd, secure);

            var addr = new SockAddrL2 { l2_family = AF_BLUETOOTH, l2_psm = psm, l2_bdaddr_type = addressType };
            WriteBdAddr(ref addr, bdAddr);

            ThrowIfNegative(connect(fd, ref addr, Marshal.SizeOf<SockAddrL2>()), "connect");
            return new L2CapHandle(fd);
        }
        catch
        {
            _ = close(fd);
            throw;
        }
    }


    /// <summary>
    /// Binds a listening L2CAP CoC socket on the local controller and returns it along with the kernel-assigned PSM.
    /// </summary>
    /// <param name="secure">True requests BT_SECURITY_MEDIUM, false BT_SECURITY_LOW.</param>
    public static (L2CapHandle Listener, ushort Psm) Listen(bool secure)
    {
        var fd = socket(AF_BLUETOOTH, SOCK_SEQPACKET, BTPROTO_L2CAP);
        ThrowIfNegative(fd, "socket");

        try
        {
            SetSecurity(fd, secure);

            // PSM=0 → kernel picks an LE dynamic PSM (>= 0x80). BDADDR=any. Public local controller.
            var addr = new SockAddrL2 { l2_family = AF_BLUETOOTH, l2_psm = 0, l2_bdaddr_type = BDADDR_LE_PUBLIC };
            ThrowIfNegative(bind(fd, ref addr, Marshal.SizeOf<SockAddrL2>()), "bind");
            ThrowIfNegative(listen(fd, 8), "listen");

            var actual = default(SockAddrL2);
            var len = Marshal.SizeOf<SockAddrL2>();
            ThrowIfNegative(getsockname(fd, ref actual, ref len), "getsockname");

            return (new L2CapHandle(fd), actual.l2_psm);
        }
        catch
        {
            _ = close(fd);
            throw;
        }
    }


    /// <summary>
    /// Blocking accept on a listening L2CAP CoC socket. Returns the accepted handle plus the
    /// remote peer's BDADDR in "AA:BB:CC:DD:EE:FF" form. Returns null when the listener has been closed.
    /// </summary>
    public static (L2CapHandle Client, string PeerAddress)? Accept(L2CapHandle listener)
    {
        var addr = default(SockAddrL2);
        var len = Marshal.SizeOf<SockAddrL2>();
        var fd = accept(listener.Fd, ref addr, ref len);
        if (fd < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            // EBADF/EINVAL after close, EINTR on signal — treat as listener stopped.
            if (errno is 9 or 22 or 4)
                return null;
            throw new IOException($"L2CAP accept failed (errno {errno})");
        }
        return (new L2CapHandle(fd), ReadBdAddr(ref addr));
    }


    static void SetSecurity(int fd, bool secure)
    {
        var sec = new BtSecurity { level = secure ? BT_SECURITY_MEDIUM : BT_SECURITY_LOW, key_size = 0 };
        ThrowIfNegative(setsockopt(fd, SOL_BLUETOOTH, BT_SECURITY, ref sec, Marshal.SizeOf<BtSecurity>()), "setsockopt(BT_SECURITY)");
    }


    static void WriteBdAddr(ref SockAddrL2 addr, string bdAddr)
    {
        // BlueZ exposes addresses as "AA:BB:CC:DD:EE:FF" (MSB first). The kernel stores the
        // six raw bytes in little-endian order (LSB first).
        var parts = bdAddr.Split(':');
        if (parts.Length != 6)
            throw new ArgumentException($"Invalid BD address '{bdAddr}'", nameof(bdAddr));

        addr.b0 = byte.Parse(parts[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        addr.b1 = byte.Parse(parts[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        addr.b2 = byte.Parse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        addr.b3 = byte.Parse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        addr.b4 = byte.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        addr.b5 = byte.Parse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }


    static string ReadBdAddr(ref SockAddrL2 addr)
        => $"{addr.b5:X2}:{addr.b4:X2}:{addr.b3:X2}:{addr.b2:X2}:{addr.b1:X2}:{addr.b0:X2}";


    static void ThrowIfNegative(int rc, string call)
    {
        if (rc < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            throw new IOException($"L2CAP {call} failed (errno {errno})");
        }
    }


    /// <summary>
    /// Owns a raw L2CAP file descriptor and exposes blocking read/write/close. Send and Receive
    /// are blocking so callers should drive them on a dedicated background task.
    /// </summary>
    public sealed class L2CapHandle : IDisposable
    {
        int fd;

        internal L2CapHandle(int fd) => this.fd = fd;
        internal int Fd => this.fd;

        public bool IsClosed => this.fd < 0;

        /// <summary>
        /// Blocking write. Returns when all <paramref name="payload"/> bytes have been queued
        /// to the kernel. SOCK_SEQPACKET preserves message boundaries — one Send is one frame
        /// up to the negotiated MTU.
        /// </summary>
        public unsafe Task SendAsync(byte[] payload, CancellationToken ct)
            => Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                if (this.IsClosed) throw new ObjectDisposedException(nameof(L2CapHandle));

                fixed (byte* p = payload)
                {
                    var rc = write(this.fd, (IntPtr)p, (nuint)payload.Length);
                    if (rc < 0)
                    {
                        var errno = Marshal.GetLastPInvokeError();
                        throw new IOException($"L2CAP write failed (errno {errno})");
                    }
                }
            }, ct);

        /// <summary>
        /// Blocking read returning one received frame (or null on clean close / EBADF).
        /// </summary>
        public unsafe byte[]? Receive()
        {
            if (this.IsClosed) return null;

            var buffer = new byte[ReadBufferSize];
            nint rc;
            fixed (byte* p = buffer)
            {
                rc = read(this.fd, (IntPtr)p, (nuint)buffer.Length);
            }
            if (rc < 0)
            {
                var errno = Marshal.GetLastPInvokeError();
                if (errno is 9 or 4) return null; // EBADF / EINTR after close
                throw new IOException($"L2CAP read failed (errno {errno})");
            }
            if (rc == 0)
                return null;

            var result = new byte[rc];
            Buffer.BlockCopy(buffer, 0, result, 0, (int)rc);
            return result;
        }

        public void Dispose()
        {
            var local = Interlocked.Exchange(ref this.fd, -1);
            if (local >= 0)
            {
                _ = shutdown(local, 2); // SHUT_RDWR — unblocks any pending read/accept
                _ = close(local);
            }
        }
    }
}
