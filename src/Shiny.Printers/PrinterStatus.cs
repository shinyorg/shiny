namespace Shiny.Printers;


/// <summary>
/// A point-in-time read of the printer's own sensors, returned by <see cref="IPrinter.GetStatus"/>. Only
/// transports with a back-channel can produce one - check <see cref="IPrinter.SupportsStatus"/> first.
/// </summary>
public sealed record PrinterStatus
{
    /// <summary>False when the printer has taken itself offline (cover open, paper out, error, feeding).</summary>
    public required bool IsOnline { get; init; }

    /// <summary>The paper cover is open.</summary>
    public required bool IsCoverOpen { get; init; }

    /// <summary>The roll is running low - the near-end sensor has tripped. Not every printer fits one.</summary>
    public required bool IsPaperNearEnd { get; init; }

    /// <summary>The roll is empty, so printing has stopped.</summary>
    public required bool IsPaperOut { get; init; }

    /// <summary>The auto-cutter has jammed or failed.</summary>
    public required bool IsCutterError { get; init; }

    /// <summary>An error the printer cannot clear itself (it needs a power cycle or service).</summary>
    public required bool HasUnrecoverableError { get; init; }

    /// <summary>An error that clears on its own, such as the print head overheating.</summary>
    public required bool HasAutoRecoverableError { get; init; }

    /// <summary>
    /// The cash-drawer sensor (kick-out connector pin 3) reads high. Whether high means open or closed depends
    /// on the drawer, so check the polarity against your drawer once.
    /// </summary>
    public required bool IsDrawerSensorHigh { get; init; }

    /// <summary>True when any of the error flags is set.</summary>
    public bool HasError => this.IsCutterError || this.HasUnrecoverableError || this.HasAutoRecoverableError;

    /// <summary>True when the printer is online with the cover shut, paper loaded and no error.</summary>
    public bool CanPrint => this.IsOnline && !this.IsCoverOpen && !this.IsPaperOut && !this.HasError;
}
