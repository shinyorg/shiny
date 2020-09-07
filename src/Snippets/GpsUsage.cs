using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;


public class GpsUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IGpsManager>();
        var result = await manager.RequestAccess(GpsRequest.Realtime(true));
        if (result == AccessState.Available)
        {
            //manager.WhenReading().Subscribe(reading =>
            //{

            //});
            await manager.StartListener(GpsRequest.Realtime(true));

            await manager.StopListener();
        }
    }
}