using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


public interface IJobManager
{
    /// <summary>
    /// Runs a one time, adhoc task - on iOS, it will initiate a background task
    /// </summary>
    /// <param name="taskName"></param>
    /// <param name="task"></param>
    void RunTask(string taskName, Func<CancellationToken, Task> task);


    /// <summary>
    /// Requests/ensures appropriate platform permissions where necessary
    /// </summary>
    /// <returns></returns>
    Task<AccessState> RequestAccess();
}
