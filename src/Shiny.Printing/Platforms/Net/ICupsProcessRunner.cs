namespace Shiny.Printing;


/// <summary>Result of running a CUPS command-line tool.</summary>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);


/// <summary>
/// Seam over launching the CUPS command-line tools (<c>lp</c>, <c>lpstat</c>). The default
/// implementation shells out via <see cref="System.Diagnostics.Process"/>; tests substitute a fake so
/// argument construction and output parsing can be verified without a print server.
/// </summary>
public interface ICupsProcessRunner
{
    /// <summary>Runs <paramref name="fileName"/> with <paramref name="arguments"/> and captures its output.</summary>
    Task<ProcessResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
}
