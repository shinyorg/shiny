using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;

namespace Shiny.BluetoothLE;


public static class Extensions
{
    public static Stream ToStream(this NSOutputStream stream) => new OutputStream(stream);
    public static Stream ToStream(this NSInputStream stream) => new InputStream(stream);


    public static async Task WaitForEvent(this NSStream stream, NSStreamEvent e, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        var handler = new EventHandler<NSStreamEventArgs>((sender, args) =>
        {
            if (args.StreamEvent == NSStreamEvent.ErrorOccurred)
            {
                tcs.TrySetException(new BleException("Error during read"));
            }
            else if (args.StreamEvent == e)
            {
                tcs.TrySetResult(true);
            }
        });
        try
        {
            stream.OnEvent += handler;
            using (cancellationToken.Register(() => tcs.SetCanceled()))
                await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            stream.OnEvent -= handler;
        }
    }
}

