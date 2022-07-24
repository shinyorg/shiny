using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Hosting.Managed;
#if ANDROID
using Shiny.BluetoothLE.Hosting.Internals;
#endif

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager
{
    readonly ILogger logger;
    readonly IEnumerable<BleGattService> gattServices;


    public BleHostingManager(
#if ANDROID
        AndroidPlatform platform,
#endif
        ILogger<IBleHostingManager> logger,
        IEnumerable<BleGattService> gattServices
    )
    {
#if ANDROID
        this.context = new GattServerContext(platform);
#endif
        this.logger = logger;
        this.gattServices = gattServices;
    }


    // TODO: save state
    public bool IsRegisteredServicesAttached { get; private set; }


    public async Task AttachRegisteredServices()
    {
        (await this.RequestAccess()).Assert();

        if (!this.services.Any())
            throw new InvalidOperationException("There are no register BLE services");

        // TODO: group services by service UUID
        // TODO: ensure no overlapping characteristic UUID
        foreach (var service in this.gattServices)
        {
            var st = service.GetType();
            var attribute = st.GetCustomAttribute(typeof(BleGattServiceAttribute)) as BleGattServiceAttribute;

            if (attribute == null)
                throw new InvalidOperationException($"{st.FullName} is not marked with a BleGattServiceAttribute");

            await this.AddService(attribute.ServiceUuid, true, sb => 
            {
                service.Characteristic = sb.AddCharacteristic(attribute.CharacteristicUuid, cb =>
                {
                    var read = st.GetMethod(nameof(BleGattService.OnRead))!.DeclaringType != typeof(BleGattService);
                    if (read)
                    {
                        // what about just returning the bytes?
                        cb.SetRead(async request =>
                        {
                            try
                            {
                                var result = await service
                                    .OnRead(request)
                                    .ConfigureAwait(false);

                                return result;
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError("Error executing BleGattService read request", ex);
                                return ReadResult.Error(GattState.Failure);
                            }
                        }, attribute.IsReadSecure);
                    }

                    var write = st.GetMethod(nameof(BleGattService.OnWrite))!.DeclaringType != typeof(BleGattService);
                    if (write)
                    {
                        cb.SetWrite(async request =>
                        {
                            try
                            {
                                var status = await service.OnWrite(request).ConfigureAwait(false);
                                return status;
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError("Error executing BleGattService write request", ex);
                                return GattState.Failure;
                            }
                        }, attribute.Write);
                    }

                    var notify = st.GetMethod(nameof(BleGattService.OnSubscriptionChanged))!.DeclaringType != typeof(BleGattService);
                    if (notify)
                    {
                        cb.SetNotification(async x =>
                        {
                            try
                            {
                                await service
                                    .OnSubscriptionChanged(x.Peripheral, x.IsSubscribing)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError("Error executing BleGattService notification subscription", ex);
                            }
                        }, attribute.Notifications);
                    }
                });

            });
        }
    }


    public void DetachRegisteredServices()
    {
        foreach (var service in this.gattServices)
        {
            var attribute = service
                .GetType()
                .GetCustomAttribute(typeof(BleGattServiceAttribute)) as BleGattServiceAttribute;

            if (attribute != null)
            {
                this.RemoveService(attribute.ServiceUuid);
            }
        }
    }
}

