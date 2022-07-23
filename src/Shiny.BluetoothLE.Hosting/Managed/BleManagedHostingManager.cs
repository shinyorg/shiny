using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Hosting.Managed;

namespace Shiny.BluetoothLE.Hosting;



public class BleManagedHostingManager
{
    readonly ILogger logger;
    readonly IBleHostingManager manager;
    //readonly IL2CapEndpointDelegate? l2capEndpoint;
    //IDisposable? l2capSub;


    public BleManagedHostingManager(
        ILogger<BleManagedHostingManager> logger,
        IBleHostingManager manager
        //IL2CapEndpointDelegate? l2capEndpoint = null
    )
    {
        this.logger = logger;
        this.manager = manager;
    }


    public void UnattachRegisteredServices()
    {

    }

    // TODO: if services were NOT removed, we need to have a startup here that auto-remaps and starts them up again

    // TODO: no DI if I do it like this
    public async Task AttachRegisteredService()
    {
        (await this.manager.RequestAccess()).Assert();


        //if (this.l2capEndpoint != null)
        //{
        //    this.l2capSub = this.manager
        //        .WhenL2CapChannelOpened(false)
        //        .Subscribe(async x =>
        //        {
        //            try
        //            {
        //                await this.l2capEndpoint.OnOpened(x);
        //            }
        //            catch (Exception ex)
        //            {
        //                this.logger.LogError("Error in L2cap user code", ex);
        //            }
        //        });
        //}
        // TODO: need advertising options
        // TODO: merge services and register each characteristic (how do I get the actual UUIDs per delegate now - smooth!) Attributes? Registration object?
    }


    public void StopServices()
    {
        // should enumerate operation UUIDs, but I doubt users will take both paths to do this stuff
        //this.manager.ClearServices();
        //this.manager.StopAdvertising();
        //this.l2capSub?.Dispose();
    }
}









//public class TestDelegate : IL2CapEndpointDelegate, ICharacteristicReadOperation, ICharacteristicWriteOperation, ICharacteristicSubscriptionOperation
//{
//    public async Task OnOpened(L2CapChannel channel)
//    {
//        var buffer = new Memory<byte>();
//        var read = await channel.OutputStream.ReadAsync(buffer);

//        while (read != -1)
//        {

//        }
//    }


//    public async Task<GattState> OnWrite(WriteRequest request)
//    {
//        //request.Characteristic.SubscribedCentrals
//        // TODO: I want to write to characteristic notification here
//        // TODO: user could maintain connection list?
//        return GattState.Success;
//    }


//    public async Task<ReadResult> OnRead(ReadRequest request)
//    {
//        return ReadResult.Success(new byte[] { 0x0 });
//    }


//    public async Task ObSubscriptionChange(CharacteristicSubscription subscription)
//    {
//        //subscription.IsSubscribing
//    }
//}

