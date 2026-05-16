using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


public interface IJob
{
    Task Run(CancellationToken cancelToken);
}
