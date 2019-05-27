using System;
using Android.Content;


namespace Shiny.Locations
{
    public interface IAndroidGeofenceManager
    {
        void ReceiveBoot();
        void Process(Intent intent);
    }
}
