using System;
namespace Shiny.Jobs;


/// <summary>
/// Result of a single job execution.
/// </summary>
/// <param name="Job">The registration that produced this result, or null if not associated with a registration.</param>
/// <param name="Exception">The exception that aborted the job, or null on success.</param>
public record JobRunResult(
    JobRegistration? Job,
    Exception? Exception
)
{
    /// <summary>
    /// Gets a value indicating whether the job completed without throwing.
    /// </summary>
    public bool Success => this.Exception == null;
}
