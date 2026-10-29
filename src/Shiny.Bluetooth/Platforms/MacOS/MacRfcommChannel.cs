using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using Shiny.Bluetooth.Infrastructure;
using static Shiny.Bluetooth.MacOS.IOBluetooth;

namespace Shiny.Bluetooth.MacOS;


/// <summary>
/// One IOBluetoothRFCOMMChannel and the stream over it. IOBluetooth pushes received data to the delegate,
/// so reads come out of a <see cref="PushStream"/>; writes go out with <c>writeAsync:</c> in MTU-sized chunks.
/// </summary>
internal sealed class MacRfcommChannel
{
    readonly RfcommChannelDelegate channelDelegate;
    readonly TaskCompletionSource<int> opened = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ConcurrentDictionary<IntPtr, TaskCompletionSource<int>> writes = new();
    NSObject? channel;
    long nextRefcon;


    public MacRfcommChannel()
    {
        this.channelDelegate = new RfcommChannelDelegate(this);
        this.Stream = new PushStream(this.Write, this.Close);
    }


    public PushStream Stream { get; }
    public IntPtr DelegateHandle => this.channelDelegate.Handle;

    /// <summary>Completes with the IOReturn of an outgoing open.</summary>
    public Task<int> Opened => this.opened.Task;


    /// <summary>Takes (and retains) the native channel - an outgoing open's out-parameter, or an incoming one's notification.</summary>
    public void Attach(IntPtr handle) => this.channel = Runtime.GetNSObject(handle);


    /// <summary>For an incoming channel, which arrives already open and without a delegate. Main thread only.</summary>
    public void AdoptIncoming(IntPtr handle)
    {
        this.Attach(handle);
        Int32_objc_msgSend_IntPtr(handle, Sel("setDelegate:"), this.DelegateHandle);
        this.opened.TrySetResult(Success);
    }


    /// <summary>The channel's remote device. Main thread only.</summary>
    public IntPtr GetDevice() => IntPtr_objc_msgSend(this.channel!.Handle, Sel("getDevice"));


    internal void OnOpenComplete(int status)
    {
        this.opened.TrySetResult(status);
        if (status != Success)
            this.Stream.Complete();
    }


    internal void OnData(ReadOnlySpan<byte> data) => this.Stream.Push(data);


    internal void OnWriteComplete(IntPtr refcon, int status)
    {
        if (this.writes.TryRemove(refcon, out var tcs))
            tcs.TrySetResult(status);
    }


    internal void OnClosed()
    {
        this.opened.TrySetResult(-1);
        this.Stream.Complete();
        foreach (var refcon in this.writes.Keys)
        {
            if (this.writes.TryRemove(refcon, out var tcs))
                tcs.TrySetResult(-1);
        }
    }


    async Task Write(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        var handle = this.channel?.Handle ?? throw new IOException("The RFCOMM channel is not open");
        var mtu = await MainThread.Run(() => UInt16_objc_msgSend(handle, Sel("getMTU"))).ConfigureAwait(false);
        if (mtu == 0)
            mtu = 127; // RFCOMM's default frame size

        var offset = 0;
        while (offset < data.Length)
        {
            // the token is only honoured between chunks - once IOBluetooth holds a buffer it has to be
            // left alone until the write completes
            ct.ThrowIfCancellationRequested();

            var length = Math.Min(mtu, data.Length - offset);
            var buffer = Marshal.AllocHGlobal(length);
            try
            {
                data.Span.Slice(offset, length).CopyTo(SpanOf(buffer, length));

                var refcon = (IntPtr)Interlocked.Increment(ref this.nextRefcon);
                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.writes[refcon] = tcs;

                var rc = await MainThread
                    .Run(() => Int32_objc_msgSend_IntPtr_UInt16_IntPtr(handle, Sel("writeAsync:length:refcon:"), buffer, (ushort)length, refcon))
                    .ConfigureAwait(false);

                if (rc != Success)
                {
                    this.writes.TryRemove(refcon, out _);
                    throw new IOException($"RFCOMM write failed ({Describe(rc)})");
                }

                var status = await tcs.Task.ConfigureAwait(false);
                if (status != Success)
                    throw new IOException($"RFCOMM write failed ({Describe(status)})");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            offset += length;
        }
    }


    static unsafe Span<byte> SpanOf(IntPtr buffer, int length) => new((void*)buffer, length);


    void Close()
    {
        var current = Interlocked.Exchange(ref this.channel, null);
        if (current == null)
            return;

        _ = MainThread.Run(() =>
        {
            Int32_objc_msgSend(current.Handle, Sel("closeChannel"));
            Int32_objc_msgSend_IntPtr(current.Handle, Sel("setDelegate:"), IntPtr.Zero);
            current.Dispose();

            // with the delegate gone rfcommChannelClosed: may never arrive, so release any writer still waiting
            this.OnClosed();
            GC.KeepAlive(this.channelDelegate);
        });
    }
}
