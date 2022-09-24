#if PLATFORM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.BluetoothLE.Hosting.Managed;
#if ANDROID
using Shiny.BluetoothLE.Hosting.Internals;
#endif

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager : IShinyStartupTask
{
    readonly ILogger logger;
    readonly IKeyValueStoreFactory keyStore;
    readonly Lazy<IEnumerable<BleGattCharacteristic>> gattChars;

    Dictionary<string, List<(BleGattCharacteristic Characteristic, BleGattCharacteristicAttribute Attribute)>>? gattServices;


    public BleHostingManager(
#if ANDROID
        AndroidPlatform platform,
#endif
        IKeyValueStoreFactory keyStore,
        ILogger<IBleHostingManager> logger,
        IServiceProvider services
    )
    {
#if ANDROID
        this.context = new GattServerContext(platform);
#endif
        this.keyStore = keyStore;
        this.logger = logger;
        this.gattChars = services.GetLazyService<IEnumerable<BleGattCharacteristic>>();
    }


    public bool IsRegisteredServicesAttached
    {
        get => this.keyStore.DefaultStore.Get<bool>(nameof(this.IsRegisteredServicesAttached), false);
        private set => this.keyStore.DefaultStore.Set(nameof(this.IsRegisteredServicesAttached), value);
    }


    public async void Start()
    {
        if (this.IsRegisteredServicesAttached)
        {
            try
            {
                await this.AttachRegisteredServices().ConfigureAwait(false);
                // TODO: what about restarting advertisement?
            }
            catch (Exception ex)
            {
                this.logger.LogWarning("Unable to reattach BLE hosted characteristics", ex);
            }
        }
    }


    public async Task AttachRegisteredServices()
    {
        (await this.RequestAccess()).Assert();

        this.gattServices ??= CollectServices(this.gattChars.Value);
        if (!this.gattServices.Any())
            throw new InvalidOperationException("There are no register BLE services");

        foreach (var service in this.gattServices)        
            await this.BuildService(service.Key, service.Value).ConfigureAwait(false);

        this.IsRegisteredServicesAttached = true;
    }


    public void DetachRegisteredServices()
    {
        if (!this.IsRegisteredServicesAttached)
            return;

        this.IsRegisteredServicesAttached = false;
        foreach (var serviceUuid in this.gattServices!.Keys)        
            this.RemoveService(serviceUuid);
        
        foreach (var ch in this.gattChars.Value)
            ch.OnStop(); // TODO: error trap this for user?
    }


    async Task BuildService(string serviceUuid, List<(BleGattCharacteristic Characteristic, BleGattCharacteristicAttribute Attribute)> list)
    {
        await this
            .AddService(serviceUuid, true, sb =>
            {
                foreach (var character in list)
                {
                    this.BuildCharacteristic(sb, character.Characteristic, character.Attribute);
                }
            })
            .ConfigureAwait(false);

        foreach (var ch in list)
            await ch.Characteristic.OnStart().ConfigureAwait(false); // TODO: error trap this for user
    }


    void BuildCharacteristic(IGattServiceBuilder sb, BleGattCharacteristic characteristic, BleGattCharacteristicAttribute attribute)
    {
        var ctype = characteristic.GetType();

        characteristic.Characteristic = sb.AddCharacteristic(attribute.CharacteristicUuid, cb =>
        {
            var read = ctype.IsMethodOverridden(nameof(BleGattCharacteristic.OnRead));
            var requester = ctype.IsMethodOverridden(nameof(BleGattCharacteristic.Request));
            var write = ctype.IsMethodOverridden(nameof(BleGattCharacteristic.OnWrite));
            var notify = ctype.IsMethodOverridden(nameof(BleGattCharacteristic.OnSubscriptionChanged));

            if (read)
            {
                // what about just returning the bytes?
                cb.SetRead(async request =>
                {
                    try
                    {
                        var result = await characteristic
                            .OnRead(request)
                            .ConfigureAwait(false);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Error executing BleGattService read request", ex);
                        return GattResult.Error(GattState.Failure);
                    }
                }, attribute.IsReadSecure);
            }

            if (requester && write)
                throw new InvalidOperationException("You cannot have a request & onwrite override for this characteristic");

            if (write)
            {
                cb.SetWrite(async request =>
                {
                    try
                    {
                        await characteristic
                            .OnWrite(request)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Error executing BleGattService write request", ex);
                        request.Respond(GattState.Failure);
                    }
                }, attribute.Write);
            }

            
            if (notify)
            {
                cb.SetNotification(async x =>
                {
                    try
                    {
                        await characteristic
                            .OnSubscriptionChanged(x.Peripheral, x.IsSubscribing)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Error executing BleGattService notification subscription", ex);
                    }
                }, attribute.Notifications);
            }

            if (requester)
            {
                cb.SetWrite(async request =>
                {
                    var writeSuccess = false;
                    try
                    {
                        if (!characteristic.Characteristic.SubscribedCentrals.Any(x => x.Uuid.Equals(request.Characteristic.Uuid, StringComparison.InvariantCultureIgnoreCase)))
                            throw new InvalidOperationException("No subscription to notification");

                        var result = await characteristic
                            .Request(request)
                            .ConfigureAwait(false);

                        writeSuccess = true;
                        await characteristic
                            .Characteristic
                            .Notify(result.Data!, request.Peripheral);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Error executing BleGattService request", ex);
                        if (!writeSuccess)
                            request.Respond(GattState.Failure);
                    }
                }, attribute.Write);
            }
        });
    }


    static Dictionary<string, List<(BleGattCharacteristic Characteristic, BleGattCharacteristicAttribute Attribute)>> CollectServices(IEnumerable<BleGattCharacteristic> gattChars)
    {
        var services = new Dictionary<string, List<(BleGattCharacteristic Characteristic, BleGattCharacteristicAttribute Attribute)>>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var ch in gattChars)
        {
            var type = ch.GetType();
            var attr = type.GetCustomAttribute(typeof(BleGattCharacteristicAttribute)) as BleGattCharacteristicAttribute;
            if (attr == null)
                throw new InvalidOperationException($"'{type.FullName}' does not have a BleGattCharacteristicAttribute defined on it");

            if (!Guid.TryParse(attr.ServiceUuid, out var _))
                throw new InvalidOperationException($"ServiceUUID on '{type.FullName}' is invalid");

            if (!Guid.TryParse(attr.CharacteristicUuid, out var _))
                throw new InvalidOperationException($"CharacteristicUUID on '{type.FullName}' is invalid");

            if (!services.ContainsKey(attr.ServiceUuid))
                services.Add(attr.ServiceUuid, new());

            services[attr.ServiceUuid].Add((ch, attr));
        }
        return services;
    }
}
#endif