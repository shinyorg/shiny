using System;
using System.IO;
using System.Reactive;

namespace Shiny.BluetoothLE.Hosting;


public record L2CapChannel(
    ushort Psm,
    string Identifier,
    //IObservable<Unit> WhenClosed, channel closed?
    Func<byte[], IObservable<Unit>> Write,
    IObservable<byte[]> DataReceived // close/complete when channel closed?
);