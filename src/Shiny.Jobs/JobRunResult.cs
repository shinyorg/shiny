using System;
namespace Shiny.Jobs;


public record JobRunResult(
    JobRegistration? Job,
    Exception? Exception
)
{
    public bool Success => this.Exception == null;
}
