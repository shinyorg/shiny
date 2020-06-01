using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    static class JobProcessor
    {
        public static async Task<bool> Process<T>(JobInfo jobInfo, IRepository repository, Func<IEnumerable<T>, CancellationToken, Task> process, CancellationToken cancelToken) where T : LocationEvent
        {
            var config = jobInfo.GetParameter<SyncConfig>(Constants.SyncConfigJobParameterKey);
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
                if (!cancelToken.IsCancellationRequested)
                { 
                    await process(batch, cancelToken);
                    foreach (var e in batch)
                        await repository.Remove<T>(e.Id);
                }
            }
            return events.Any();
        }
    }
}
