namespace Shiny.Printing;


/// <summary>The result of submitting a <see cref="PrintJob"/>.</summary>
/// <param name="Status">What happened.</param>
/// <param name="PrinterId">The printer the job went to, when known.</param>
/// <param name="Error">A description when <see cref="Status"/> is <see cref="PrintStatus.Failed"/>.</param>
public sealed record PrintResult(PrintStatus Status, string? PrinterId = null, string? Error = null)
{
    /// <summary>True when the job completed or was spooled successfully.</summary>
    public bool IsSuccess => this.Status is PrintStatus.Completed or PrintStatus.Submitted;

    internal static PrintResult Completed(string? printerId = null) => new(PrintStatus.Completed, printerId);
    internal static PrintResult Submitted(string? printerId = null) => new(PrintStatus.Submitted, printerId);
    internal static PrintResult Cancelled() => new(PrintStatus.Cancelled);
    internal static PrintResult Failed(string error) => new(PrintStatus.Failed, Error: error);
}
