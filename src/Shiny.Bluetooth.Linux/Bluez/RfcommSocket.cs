using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Shiny.Bluetooth.Bluez;


/// <summary>
/// The kernel side of RFCOMM. BlueZ does the connecting and accepting (through <c>Profile1</c>) and
/// hands the connected socket over as a file descriptor; this reads and writes it, and finds a free
/// channel for a server profile to claim.
/// </summary>
internal static partial class RfcommSocket
{
    // <sys/socket.h> / <bluetooth/bluetooth.h> / <bluetooth/rfcomm.h>
    const int AF_BLUETOOTH = 31;
    const int SOCK_STREAM = 1;
    const int BTPROTO_RFCOMM = 3;
    const int SHUT_RDWR = 2;
    const int EINTR = 4;
    const int EBADF = 9;
    const int ENOTCONN = 107;
    const int ECONNRESET = 104;


    [LibraryImport("libc", EntryPoint = "socket", SetLastError = true)]
    private static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc", EntryPoint = "bind", SetLastError = true)]
    private static partial int bind(int sockfd, ref SockAddrRc addr, int addrlen);

    [LibraryImport("libc", EntryPoint = "listen", SetLastError = true)]
    private static partial int listen(int sockfd, int backlog);

    [LibraryImport("libc", EntryPoint = "getsockname", SetLastError = true)]
    private static partial int getsockname(int sockfd, ref SockAddrRc addr, ref int addrlen);

    [LibraryImport("libc", EntryPoint = "shutdown", SetLastError = true)]
    internal static partial int shutdown(int sockfd, int how);

    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    internal static partial int close(int fd);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    private static unsafe partial nint read(int fd, byte* buf, nuint count);

    [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
    private static unsafe partial nint write(int fd, byte* buf, nuint count);


    // struct sockaddr_rc { sa_family_t rc_family; bdaddr_t rc_bdaddr; uint8_t rc_channel; }
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)]
    private struct SockAddrRc
    {
        public ushort rc_family;
        public byte b0, b1, b2, b3, b4, b5;
        public byte rc_channel;
    }


    /// <summary>
    /// Finds an RFCOMM channel nobody is listening on. BlueZ needs an explicit channel to publish a
    /// server profile on, and the kernel only allocates one on listen() - so a throwaway socket is bound
    /// to channel 0, listened on to make the kernel pick, read back and closed. Another process can
    /// claim the channel in the moment between, in which case registering the profile fails.
    /// </summary>
    public static byte FindFreeChannel()
    {
        var fd = socket(AF_BLUETOOTH, SOCK_STREAM, BTPROTO_RFCOMM);
        if (fd < 0)
            throw new BluetoothException($"Could not open an RFCOMM socket (errno {Marshal.GetLastPInvokeError()}) - is the bluetooth kernel module loaded?");

        try
        {
            var size = Marshal.SizeOf<SockAddrRc>();
            var addr = new SockAddrRc { rc_family = AF_BLUETOOTH, rc_channel = 0 };
            ThrowIfFailed(bind(fd, ref addr, size), "bind");
            ThrowIfFailed(listen(fd, 1), "listen");

            var actual = default(SockAddrRc);
            ThrowIfFailed(getsockname(fd, ref actual, ref size), "getsockname");
            if (actual.rc_channel == 0)
                throw new BluetoothException("Every RFCOMM channel (1-30) is in use");

            return actual.rc_channel;
        }
        finally
        {
            _ = close(fd);
        }
    }


    static void ThrowIfFailed(int rc, string call)
    {
        if (rc < 0)
            throw new BluetoothException($"RFCOMM {call} failed (errno {Marshal.GetLastPInvokeError()})");
    }


    /// <summary>Blocking read; 0 at end of stream, including when the socket is shut down under it.</summary>
    public static unsafe int Read(int fd, Span<byte> buffer)
    {
        while (true)
        {
            nint rc;
            fixed (byte* p = buffer)
                rc = read(fd, p, (nuint)buffer.Length);

            if (rc >= 0)
                return (int)rc;

            var errno = Marshal.GetLastPInvokeError();
            if (errno is EBADF or ENOTCONN or ECONNRESET)
                return 0;

            if (errno != EINTR)
                throw new IOException($"RFCOMM read failed (errno {errno})");
        }
    }


    /// <summary>Blocking write of the whole buffer.</summary>
    public static unsafe void Write(int fd, ReadOnlySpan<byte> buffer)
    {
        while (buffer.Length > 0)
        {
            nint rc;
            fixed (byte* p = buffer)
                rc = write(fd, p, (nuint)buffer.Length);

            if (rc >= 0)
            {
                buffer = buffer[(int)rc..];
            }
            else
            {
                var errno = Marshal.GetLastPInvokeError();
                if (errno != EINTR)
                    throw new IOException($"RFCOMM write failed (errno {errno})");
            }
        }
    }


    public static void Shutdown(int fd) => _ = shutdown(fd, SHUT_RDWR);
}


/// <summary>
/// A file descriptor received over D-Bus. Tmds.DBus hands a unix fd out as any SafeHandle with a
/// parameterless constructor, taking ownership away from the message.
/// </summary>
internal sealed class BluezFileDescriptor : SafeHandleZeroOrMinusOneIsInvalid
{
    public BluezFileDescriptor() : base(true) { }

    public int Fd => (int)this.handle;

    protected override bool ReleaseHandle() => RfcommSocket.close((int)this.handle) == 0;
}


/// <summary>
/// A connected RFCOMM socket as a <see cref="Stream"/>. Reads and writes are blocking syscalls run off
/// the thread pool; disposing shuts the socket down, which is what unblocks a read in progress.
/// </summary>
internal sealed class RfcommStream(BluezFileDescriptor descriptor, Action? onClose) : Stream
{
    int closed;

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }


    public override int Read(byte[] buffer, int offset, int count) => this.Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(this.closed == 1, this);
        return buffer.Length == 0 ? 0 : RfcommSocket.Read(descriptor.Fd, buffer);
    }

    // a blocked read cannot be cancelled by the token - only by disposing the stream
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => Task.Run(() => this.Read(buffer, offset, count), ct);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        => new(Task.Run(() => this.Read(buffer.Span), ct));


    public override void Write(byte[] buffer, int offset, int count) => this.Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(this.closed == 1, this);
        RfcommSocket.Write(descriptor.Fd, buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => Task.Run(() => this.Write(buffer, offset, count), ct);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        => new(Task.Run(() => this.Write(buffer.Span), ct));


    public override void Flush() { }
    public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();


    /// <summary>BlueZ's RequestDisconnection - shut the socket so a pending read returns end of stream.</summary>
    public void ShutdownFromRemote()
    {
        if (this.closed == 0)
            RfcommSocket.Shutdown(descriptor.Fd);
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref this.closed, 1) == 0)
        {
            RfcommSocket.Shutdown(descriptor.Fd);
            descriptor.Dispose();
            onClose?.Invoke();
        }
        base.Dispose(disposing);
    }
}
