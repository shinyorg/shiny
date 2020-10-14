using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public interface IMotionActivityManager
    {
        /// <summary>
        /// Requests access to use the necessary platform infrastructure
        /// </summary>
        /// <returns></returns>
        Task<AccessState> RequestAccess();

        /// <summary>
        ///
        /// </summary>
        /// <param name="start">Start of time range</param>
        /// <param name="end">End time range - assumes now if not passed</param>
        /// <returns></returns>
        Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null);

        /// <summary>
        /// This will only work in the foreground on iOS
        /// </summary>
        /// <returns></returns>
        IObservable<MotionActivityEvent> WhenActivityChanged();
    }
}
