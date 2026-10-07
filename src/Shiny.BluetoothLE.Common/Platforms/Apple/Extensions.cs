using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public static class Extensions
{
    public static bool IsConnected(this NSStream stream) =>
        stream.Status == NSStreamStatus.Open ||
        stream.Status == NSStreamStatus.Reading ||
        stream.Status == NSStreamStatus.Writing;


    public static IObservable<NSStreamEvent> WhenEvent(this NSStream stream, bool throwError = false) => Observable.Create<NSStreamEvent>(ob =>
    {
        var handler = new EventHandler<NSStreamEventArgs>((sender, args) =>
        {
            if (args.StreamEvent == NSStreamEvent.ErrorOccurred && throwError)
                ob.OnError(stream.ToError());
            else
                ob.OnNext(args.StreamEvent);
        });

        stream.OnEvent += handler;
        return () => stream.OnEvent -= handler;
    });


    static BleException ToError(this NSStream stream)
    {
        var msg = stream.Error?.LocalizedFailureReason ?? "Unknown stream error";
        return new BleException(msg);
    }


    /// <summary>
    /// Opens both streams of an L2CAP channel and wraps them as an <see cref="L2CapChannel"/>.
    /// </summary>
    /// <remarks>
    /// NSStream only raises events when it is scheduled on a run loop. Unscheduled, HasBytesAvailable and
    /// HasSpaceAvailable never fire, so reads see nothing and writes wait forever.
    /// </remarks>
    public static L2CapChannel ToL2CapChannel(this CBL2CapChannel channel)
    {
        var runLoop = NSRunLoop.Main;
        channel.InputStream.Schedule(runLoop, NSRunLoopMode.Common);
        channel.OutputStream.Schedule(runLoop, NSRunLoopMode.Common);
        channel.InputStream.Open();
        channel.OutputStream.Open();

        return new L2CapChannel(
            channel.Psm,
            channel.Peer.Identifier.ToString(),
            data => Observable.FromAsync(ct => channel.OutputStream.WriteAsync(data, 0, data.Length, ct)),
            channel.InputStream.ListenForData(),
            () =>
            {
                channel.InputStream.Close();
                channel.OutputStream.Close();
                channel.InputStream.Unschedule(runLoop, NSRunLoopMode.Common);
                channel.OutputStream.Unschedule(runLoop, NSRunLoopMode.Common);
            }
        );
    }


    public static async Task WriteAsync(this NSOutputStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        while (count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!stream.HasSpaceAvailable())
            {
                // HasSpaceAvailable is edge triggered - listen first, then check again so a signal in between isn't lost
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var wait = stream
                    .WhenEvent()
                    .Where(x => x is NSStreamEvent.HasSpaceAvailable or NSStreamEvent.ErrorOccurred or NSStreamEvent.EndEncountered)
                    .Take(1)
                    .ToTask(cts.Token);

                if (stream.HasSpaceAvailable())
                {
                    cts.Cancel();
                }
                else
                {
                    var ev = await wait.ConfigureAwait(false);
                    if (ev == NSStreamEvent.ErrorOccurred)
                        throw stream.ToError();

                    if (ev == NSStreamEvent.EndEncountered)
                        throw new BleException("L2CAP channel closed");
                }
            }

            var written = (int)stream.Write(buffer, offset, (nuint)count);
            if (written < 0)
                throw stream.ToError();

            offset += written;
            count -= written;
        }
    }


    public static IObservable<byte[]> ListenForData(this NSInputStream stream) => Observable.Create<byte[]>(ob =>
    {
        var buffer = new byte[8192];

        void Drain()
        {
            // the first drain runs on the subscriber's thread, events arrive on the run loop's
            lock (buffer)
            {
                while (stream.HasBytesAvailable())
                {
                    var read = (int)stream.Read(buffer, 0, (nuint)buffer.Length);
                    if (read <= 0)
                        return;

                    var payload = new byte[read];
                    Array.Copy(buffer, 0, payload, 0, read);
                    ob.OnNext(payload);
                }
            }
        }

        // listen before the first drain so bytes that arrive in between still raise an event
        var comp = new CompositeDisposable();
        comp.Add(stream
            .WhenEvent()
            .Where(x => x == NSStreamEvent.HasBytesAvailable)
            .Subscribe(_ => Drain())
        );

        comp.Add(stream
            .WhenEvent()
            .Where(x => x == NSStreamEvent.ErrorOccurred)
            .Subscribe(_ => ob.OnError(stream.ToError()))
        );

        comp.Add(stream
            .WhenEvent()
            .Where(x => x == NSStreamEvent.EndEncountered)
            .Subscribe(_ => ob.OnCompleted())
        );
        Drain();

        return comp;
    });
}

