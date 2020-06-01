using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Logging;


namespace Shiny.Locations.Sync.Infrastructure
{
    static class JobProcessor
    {
        public static async Task<bool> Process<T>(JobInfo jobInfo, IRepository repository, Func<IEnumerable<T>, CancellationToken, Task> process, CancellationToken cancelToken) where T : LocationEvent
        {
            var config = jobInfo.GetSyncConfig();
            var events = await repository.GetAll<T>();
            var list = config.SortMostRecentFirst 
                ? events.OrderByDescending(x => x.DateCreated) 
                : events.OrderBy(x => x.DateCreated);

            //if (config.ExpirationTime != null)
            //{
            //    // TODO: if expired, delete it - could also let processor deal with this
            //    //list = list.Where(x => x.DateCreated.Add(expiryTime) > DateTime.UtcNow);
            //}
            foreach (var batch in list.Page(config.BatchSize))
            {
                var batchProcessed = false;

                // configure how aggressive this need to be?
                while (!cancelToken.IsCancellationRequested && !batchProcessed)
                { 
                    try
                    { 
                        await process(batch, cancelToken);
                        batchProcessed = true;

                        foreach (var e in batch)
                            await repository.Remove<T>(e.Id);
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(1000); // TODO: retry time
                        Log.Write(ex);
                    }
                }
            }
            return events.Any();
        }
    }
}
