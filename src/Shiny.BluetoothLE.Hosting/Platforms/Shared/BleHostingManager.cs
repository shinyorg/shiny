#if PLATFORM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.BluetoothLE.Hosting.Managed;
#if APPLE
using CoreBluetooth;
#endif
#if ANDROID
using Java.Util;
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
        this.logger = logger;
        this.keyStore = keyStore;
        this.gattChars = services.GetLazyService<IEnumerable<BleGattCharacteristic>>();
    }


    const string REG_KEY = "BleHostingManager.IsRegisteredServicesAttached";
    public bool IsRegisteredServicesAttached
    {
        get => this.keyStore.DefaultStore.Get(REG_KEY, false);
        set
        {
            if (value)
                this.keyStore.DefaultStore.Set(REG_KEY, true);
            else
                this.keyStore.DefaultStore.Remove(REG_KEY);
        }
    }


    public async void Start()
    {
        if (this.IsRegisteredServicesAttached)
        {
            try
            {
                this.IsRegisteredServicesAttached = false; // temp off to allow reprocess
                await this.AttachRegisteredServices().ConfigureAwait(false);
                // TODO: what about restarting advertisement?
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Unable to reattach BLE hosted characteristics");
            }
        }
    }


    public async Task AttachRegisteredServices()
    {
        if (this.IsRegisteredServicesAttached)
            return;

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

        foreach (var serviceUuid in this.gattServices!.Keys)        
            this.RemoveService(serviceUuid);
        
        foreach (var ch in this.gattChars.Value)
            ch.OnStop(); // TODO: error trap this for user?

        this.IsRegisteredServicesAttached = true;
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


    static bool IsMethodOverridden(Type type, string methodName, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
    {
        var method = type.GetMethod(methodName, flags);
        if (method == null)
            throw new InvalidOperationException($"No method named '{methodName}' was found on type '{type.FullName}'");

        // var baseType = method.GetBaseDefinition().DeclaringType;
        var result = !method.DeclaringType!.FullName.Equals(method.GetBaseDefinition().DeclaringType?.FullName);

        return result;
    }

    void BuildCharacteristic(IGattServiceBuilder sb, BleGattCharacteristic characteristic, BleGattCharacteristicAttribute attribute)
    {
        var ctype = characteristic.GetType();

        characteristic.Characteristic = sb.AddCharacteristic(attribute.CharacteristicUuid, cb =>
        {
            var read = IsMethodOverridden(ctype, nameof(BleGattCharacteristic.OnRead));
            var requester = IsMethodOverridden(ctype, nameof(BleGattCharacteristic.Request));
            var write = IsMethodOverridden(ctype, nameof(BleGattCharacteristic.OnWrite));
            var notify = IsMethodOverridden(ctype, nameof(BleGattCharacteristic.OnSubscriptionChanged));

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
                        this.logger.LogError(ex, "Error executing BleGattService notification subscription");
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
                        this.logger.LogError(ex, "Error executing BleGattService request");
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

            if (!IsValidUuid(attr.ServiceUuid))
                throw new InvalidOperationException($"ServiceUUID on '{type.FullName}' is invalid");

            if (!IsValidUuid(attr.CharacteristicUuid))
                throw new InvalidOperationException($"CharacteristicUUID on '{type.FullName}' is invalid");

            if (!services.ContainsKey(attr.ServiceUuid))
                services.Add(attr.ServiceUuid, new());

            services[attr.ServiceUuid].Add((ch, attr));
        }
        return services;
    }


    static bool IsValidUuid(string value)
    {
        if (value.IsEmpty())
            return false;

#if APPLE
        try
        { 
            CBUUID.FromString(value);
            return true;
        }
        catch
        {
            return false;
        }
#elif ANDROID
        try
        { 
            UUID.FromString(value);
            return true;
        }
        catch
        {
            return false;
        }
#else
        return Guid.TryParse(value, out var _);
#endif
    }
}
#endif