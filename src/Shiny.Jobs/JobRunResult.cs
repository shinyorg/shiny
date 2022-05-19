using System;
namespace Shiny.Jobs;


public record JobRunResult(
    JobInfo? JobInfo, 
    Exception? Exception
)
{
    public bool Success => this.Exception == null;
};