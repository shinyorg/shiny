using System;
using System.IO;

namespace Shiny.BluetoothLE.Hosting;


public record L2CapChannel(
    ushort Psm,
    Stream InputStream,
    Stream OutputStream
);