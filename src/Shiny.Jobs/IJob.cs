using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


/// <summary>
/// Represents a background job that can be registered and executed by the job manager.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Executes the job's work.
    /// </summary>
    /// <param name="cancelToken">Token used to signal that the job should stop early.</param>
    Task Run(CancellationToken cancelToken);
}
