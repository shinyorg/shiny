using System;
using Shiny.Locations;


namespace Shiny.Notifications
{
    public class GeofenceTrigger
    {
        public bool Repeat { get; set; }

        /// <summary>
        /// The center of the geofence
        /// </summary>
        public Position? Center { get; }

        /// <summary>
        /// The radius of the region.
        /// </summary>
        public Distance? Radius { get; }


        public void AssertValid()
        {
            if (this.Radius == null)
                throw new InvalidOperationException("Radius is not set");

            if (this.Center == null)
                throw new InvalidOperationException("Center is not set");
        }
    }
}
