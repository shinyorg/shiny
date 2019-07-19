using System;
using System.Threading.Tasks;
using Android.Content;


namespace Shiny.Locations
{
    public interface IAndroidGeofenceManager
    {
        Task Process(Intent intent);
    }
}
