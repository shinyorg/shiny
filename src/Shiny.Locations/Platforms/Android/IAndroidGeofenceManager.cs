using System;
using System.Threading.Tasks;
using Android.Content;


namespace Shiny.Locations
{
    public interface IAndroidGeofenceManager
    {
        Task ReceiveBoot();
        Task Process(Intent intent);
    }
}
