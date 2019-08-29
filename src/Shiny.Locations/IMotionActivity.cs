using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public interface IMotionActivity
    {
        /// <summary>
        ///
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset end);

        /// <summary>
        /// This will only work in the foreground on iOS
        /// </summary>
        /// <returns></returns>
        IObservable<MotionActivityEvent> WhenActivityChanged();
    }
}
