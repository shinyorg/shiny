using Shiny.Locations;


namespace Shiny.Notifications
{
    public class Geofence
    {
        public bool Repeat { get; set; }

        /// <summary>
        /// The center of the geofence
        /// </summary>
        public Position Center { get; }

        /// <summary>
        /// The radius of the region.
        /// </summary>
        public Distance Radius { get; }
    }
}
