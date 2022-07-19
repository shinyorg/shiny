//// the managed services will allow for proper background ops
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.DependencyInjection.Extensions;
//using Microsoft.Extensions.Logging;

//namespace Shiny.BluetoothLE.Hosting;


//public interface IBleManagedHostingManager
//{
//    // TODO: readonly list of services this way I can manage the context to see and send to subscribed centrals
//    Task StartServices();
//    void StopServices();
//}


//internal class CharacteristicOperationRegistration
//{
//    public void Add(string serviceUuid, string characteristicUuid, Type operationTypes)
//    {

//    }


//    public IReadOnlyList<(string ServiceUuid, IDictionary<string, Type> Operations)> GetOps()
//    {
//        return null;
//    }
//}

//public class BleManagedHostingManager : IBleManagedHostingManager
//{
//    readonly ILogger logger;
//    readonly IBleHostingManager manager;
//    readonly IEnumerable<ICharacteristicOperation> operations;
//    readonly IL2CapEndpointDelegate? l2capEndpoint;
//    IDisposable? l2capSub;


//    public BleManagedHostingManager(
//        ILogger<BleManagedHostingManager> logger,
//        IBleHostingManager manager,
//        IEnumerable<ICharacteristicOperation> operations,
//        IL2CapEndpointDelegate? l2capEndpoint = null
//    )
//    {
//        this.logger = logger;
//        this.manager = manager;
//        this.operations = operations;
//        this.l2capEndpoint = l2capEndpoint;
//    }


//    public async Task StartServices()
//    {
//        (await this.manager.RequestAccess()).Assert();

//        if (this.l2capEndpoint != null)
//        {
//            this.l2capSub = this.manager
//                .WhenL2CapChannelOpened(false)
//                .Subscribe(async x =>
//                {
//                    try
//                    {
//                        await this.l2capEndpoint.OnOpened(x);
//                    }
//                    catch (Exception ex)
//                    {
//                        this.logger.LogError("Error in L2cap user code", ex);
//                    }
//                });
//        }
//        // TODO: need advertising options
//        // TODO: merge services and register each characteristic (how do I get the actual UUIDs per delegate now - smooth!) Attributes? Registration object?
//    }


//    public void StopServices()
//    {
//        // should enumerate operation UUIDs, but I doubt users will take both paths to do this stuff
//        this.manager.ClearServices(); 
//        this.manager.StopAdvertising();
//        this.l2capSub?.Dispose();
//    }
//}

//// TODO: how to control advertising with this model?
//    // TODO: advertising should be controllable (and usable without gatt)
//// TODO: in these models I could just control advertising on behalf of the user?  many things can go wrong with this - MUST force a serviceUUID - can't use device name

//// TODO: does the startserver take advertising options (best option so far) and starts/adds all services.  Should L2cap open as well?  maybe a secondary arg?
//public static class ServiceCollectionExtensions
//{
//    // TODO: BleHostingManager should be able to start (add)/stop (remove) service
//        // TODO: could do auto start (or auto restart) but this is dangerous if a permission request is about to take place
//    public static void AddBleHostedL2Channel<TDelegate>(this IServiceCollection services) where TDelegate : IL2CapEndpointDelegate
//    {
//        // TODO: I could cheat and put some sort of context object in the services and pull it out to add services
//    }

//    public static void AddBleHostedService<TDelegate>(this IServiceCollection services, string serviceUuid, string characteristicUuid) where TDelegate : ICharacteristicOperation
//    {
//        //services.SingleOrDefault(x => x.ImplementationInstance is CharacteristicOperationRegistration)?.ImplementationInstance;
//        // TODO: merge ALL services on build?  use a shiny startup service?
//        // TODO: services can overlap, characteristics cannot be repeated (within the same service?
//        // TODO: what about encryption, permissions?  attributes?
//        // TODO: what if I want to monitor connections or send to subscribed centrals on a timer?
//            // TODO: what if I want to do this externally?


//        // TODO: could use a class instead of an interface to allow access to a Context
//    }


//    public static void AddBleManagedHosting(this IServiceCollection services)
//    {
//        services.AddBluetoothLeHosting();
//        if (!services.Any(x => x.ImplementationType == typeof(BleManagedHostingManager)))
//            services.AddShinyService<BleManagedHostingManager>();
//    }
//}

//// TODO: you can only have 1
//public interface IL2CapEndpointDelegate
//{
//    // TODO: secure?
//    // TODO: when this is done, do we close the channel?  users will likely loop on the thread or a timer/infinite thread of some sort
//    Task OnOpened(L2CapChannel channel);
//}



//public interface ICharacteristicOperation { }
//public interface ICharacteristicWriteOperation : ICharacteristicOperation
//{
//    Task<GattState> OnWrite(WriteRequest request);
//}
//public interface ICharacteristicReadOperation : ICharacteristicOperation
//{
//    Task<ReadResult> OnRead(ReadRequest request);
//}
//public interface ICharacteristicSubscriptionOperation : ICharacteristicOperation
//{
//    Task ObSubscriptionChange(CharacteristicSubscription subscription);
//}

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

