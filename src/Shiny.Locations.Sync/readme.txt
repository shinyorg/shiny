---------------------
Shiny.Locations.Sync
---------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

Sync Samples: https://github.com/shinyorg/shinysamples/tree/master/Samples/LocationSync

---------------------
Setup
---------------------

Be sure to follow general setup here 
https://github.com/shinyorg/shiny/blob/master/src/Shiny.Core/readme.txt
and
https://github.com/shinyorg/shiny/blob/master/src/Shiny.Locations/readme.txt


---------------------
Shiny Startup
---------------------

services.UseGeofencingSync<LocationSyncDelegates>();

services.UseGpsSync<LocationSyncDelegates>();


---
Sample Delegate
---

public class LocationSyncDelegates : IGeofenceSyncDelegate, IGpsSyncDelegate
{
    public async Task Process(IEnumerable<GpsEvent> events, CancellationToken cancelToken) 
    {
        // send to your server here
        // you don't have to error trap here, shiny will retry it again
    }


    public async Task Process(IEnumerable<GeofenceEvent> events, CancellationToken cancelToken) 
    {
        // send to your server here
        // you don't have to error trap here, shiny will retry it again
    }
}