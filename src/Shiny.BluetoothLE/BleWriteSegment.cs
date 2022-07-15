namespace Shiny.BluetoothLE;

public record BleWriteSegment(
    byte[] Chunk,
    int Position,
    int TotalLength
);