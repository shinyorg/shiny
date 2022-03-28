namespace Shiny.Locations
{

    // helps with auto pausing on iOS
    //public enum GpsActivityType
    //{
    //    Airborne,
    //    Automotive,
    //    Fitness,
    //    Other
    //}


    public enum GpsAccuracy
    {
        //Reduced,

        /// <summary>
        /// 3km
        /// </summary>
        Lowest = 1,

        /// <summary>
        /// 1km
        /// </summary>
        Low = 2,
        
        /// <summary>
        /// 100 meters
        /// </summary>
        // 100 meters
        Normal = 3,

        /// <summary>
        /// 10 meters
        /// </summary>
        High = 4,

        /// <summary>
        /// Immediate results
        /// </summary>
        Highest = 5
    }
}
