using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny
{
    public class TaskQueue
    {
        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);


        public async Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            await this.semaphore.WaitAsync();
            try
            {
                return await taskGenerator();
            }
            finally
            {
                this.semaphore.Release();
            }
        }


        public async Task Enqueue(Func<Task> taskGenerator)
        {
            await this.semaphore.WaitAsync();
            try
            {
                await taskGenerator();
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }
}
