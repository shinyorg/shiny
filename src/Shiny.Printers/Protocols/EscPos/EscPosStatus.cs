namespace Shiny.Printers.Protocols.EscPos;


/// <summary>
/// ESC/POS real-time status (<c>DLE EOT n</c>). The printer answers these even while offline, so they can say
/// why it stopped. Each reply is one byte with bits 0, 1, 4 and 7 fixed at 0, 1, 1, 0.
/// </summary>
static class EscPosStatus
{
    const byte DLE = 0x10;
    const byte EOT = 0x04;

    /// <summary>n = 1 printer, 2 offline cause, 3 error cause, 4 roll paper sensor - one reply byte each, in order.</summary>
    public static readonly byte[] Request =
    [
        DLE, EOT, 1,
        DLE, EOT, 2,
        DLE, EOT, 3,
        DLE, EOT, 4
    ];

    public const int ResponseLength = 4;


    public static PrinterStatus Parse(ReadOnlySpan<byte> response)
    {
        if (response.Length != ResponseLength)
            throw new InvalidDataException($"Expected {ResponseLength} ESC/POS status bytes, got {response.Length}.");

        foreach (var b in response)
        {
            if ((b & 0x93) != 0x12)
                throw new InvalidDataException($"0x{b:X2} is not an ESC/POS status byte - the printer may not support DLE EOT, or has automatic status back enabled.");
        }

        byte printer = response[0], offline = response[1], error = response[2], paper = response[3];
        return new PrinterStatus
        {
            IsOnline = (printer & 0x08) == 0,
            IsDrawerSensorHigh = (printer & 0x04) != 0,
            IsCoverOpen = (offline & 0x04) != 0,
            IsPaperOut = (offline & 0x20) != 0 || (paper & 0x60) != 0,
            IsPaperNearEnd = (paper & 0x0C) != 0,
            IsCutterError = (error & 0x08) != 0,
            HasUnrecoverableError = (error & 0x20) != 0,
            HasAutoRecoverableError = (error & 0x40) != 0
        };
    }
}
