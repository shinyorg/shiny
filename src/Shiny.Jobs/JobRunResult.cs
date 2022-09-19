using System;
namespace Shiny.Jobs;


public record JobRunResult(
    JobInfo? Job, 
    Exception? Exception
)
{
    public bool Success => this.Exception == null;
};