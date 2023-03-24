using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shiny.Locations;


public static class MotionActivityExtensions
{
    ///// <summary>
    ///// Queries for the most current event
    ///// </summary>
    ///// <param name="activity"></param>
    ///// <param name="maxAge">Timespan containing max age of "current"</param>
    ///// <returns></returns>
    //public static async Task<MotionActivityEvent?> GetLastActivity(this IMotionActivityManager activity)
    //{
    //    var result = (await activity.Query(null, end))
    //        .OrderByDescending(x => x.Timestamp)
    //        .FirstOrDefault();

    //    return result;
    //}



    /// <summary>
    /// Queries for activities for an entire day (beginning to end)
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    public static Task<IList<MotionActivityEvent>> QueryByDate(this IMotionActivityManager activity, DateTimeOffset date)
    {
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        return activity.Query(date.Date, endOfDay);
    }
}
