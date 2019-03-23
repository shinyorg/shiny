using System;


namespace Shiny.Locations
{
    public class GpsRequest
    {
        public bool UseBackground { get; set; }

        /// <summary>
        /// iOS: Minimum horizontal distance to travel in meters
        /// </summary>
        //public double DistanceFilter { get; set; }

        /// <summary>
        /// Lower values improve battery
        /// </summary>
        //public double? DesiredAccuracy { get; set; }


        /// <summary>
        ///
        /// </summary>
        //public object ActivityType { get; set; }


        /// <summary>
        ///
        /// </summary>
        //public Type BackgroundDelegate { get; set; }
    }
}
