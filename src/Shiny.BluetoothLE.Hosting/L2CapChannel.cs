using System;
using System.IO;
using System.Reactive;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// An open L2CAP channel for streaming bytes to/from a connected central.
/// </summary>
/// <param name="Psm">The protocol/service multiplexer the channel was opened on.</param>
/// <param name="Identifier">A platform-specific identifier for the channel instance.</param>
/// <param name="Write">Function used to write bytes to the remote endpoint. Returns an observable that completes when the bytes have been queued.</param>
/// <param name="DataReceived">Observable stream of bytes received from the remote endpoint.</param>
public record L2CapChannel(
    ushort Psm,
    string Identifier,
    //IObservable<Unit> WhenClosed, channel closed?
    Func<byte[], IObservable<Unit>> Write,
    IObservable<byte[]> DataReceived // close/complete when channel closed?
);
