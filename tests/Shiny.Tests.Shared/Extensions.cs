using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Tests
{
    static class Extensions
    {
        public static Task WithTimeout(this Task task, int seconds, CancellationToken? cancelToken = null) =>
            Observable
                .FromAsync(() => task)
                .Timeout(TimeSpan.FromSeconds(seconds))
                .ToTask(cancelToken ?? CancellationToken.None);
    }
}
