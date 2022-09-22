using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
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


    public static Task WaitForSpaceAvailable(this NSOutputStream stream, CancellationToken cancellationToken) => stream
        .WhenEvent()
        .Where(x => x == NSStreamEvent.HasSpaceAvailable)
        .Take(1)
        .ToTask(cancellationToken);


    public static async Task WriteAsync(this NSOutputStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await stream.WaitForSpaceAvailable(cancellationToken).ConfigureAwait(false);

        var stillToSend = new byte[buffer.Length - offset];
        buffer.CopyTo(stillToSend, offset);

        while (count > 0 && !cancellationToken.IsCancellationRequested)
        {
            await stream.WaitForSpaceAvailable(cancellationToken).ConfigureAwait(false);
            var bytesWritten = stream.Write(buffer, offset, (uint)count);

            if (bytesWritten == -1)
            {
                throw new InvalidOperationException("Write error: -1 returned");
            }
            else if (bytesWritten > 0)
            {
                Console.WriteLine("Bytes written: " + bytesWritten.ToString());
                count -= (int)bytesWritten;
                if (0 == count)
                    break;

                var temp = new List<byte>();
                for (var i = bytesWritten; i < stillToSend.Length; i++)
                {
                    temp.Add(stillToSend[i]);
                }
                stillToSend = temp.ToArray();
            }
        }
    }


    public static IObservable<byte[]> ListenForData(this NSInputStream stream) => Observable.Create<byte[]>(ob =>
    {
        var buffer = new byte[8192];
        var read = 0;

        if (stream.HasBytesAvailable())
        {
            read = (int)stream.Read(buffer, 0, (nuint)buffer.Length);
            if (read > 0)
                ob.OnNext(buffer);
        }

        var comp = new CompositeDisposable();
        stream
            .WhenEvent()
            .Where(x => x == NSStreamEvent.HasBytesAvailable)
            .Subscribe(_ =>
            {
                read = (int)stream.Read(buffer, 0, (nuint)buffer.Length);
                if (read > 0)
                    ob.OnNext(buffer);
            })
            .DisposedBy(comp);

        stream
            .WhenEvent()
            .Where(x => x == NSStreamEvent.ErrorOccurred)
            .Subscribe(_ => ob.OnError(stream.ToError()));

        return comp;
    });
}

