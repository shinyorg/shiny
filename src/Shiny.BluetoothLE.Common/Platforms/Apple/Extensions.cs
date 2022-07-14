using System;
using System.IO;
using Foundation;

namespace Shiny.BluetoothLE;


public static class Extensions
{
    public static Stream ToStream(this NSOutputStream stream) => new OutputStream(stream);
    public static Stream ToStream(this NSInputStream stream) => new InputStream(stream);
}

