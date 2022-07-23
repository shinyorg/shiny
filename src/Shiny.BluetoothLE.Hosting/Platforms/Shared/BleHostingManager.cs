using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Hosting.Managed;

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager
{
    readonly ILogger logger;
    readonly IEnumerable<BleGattService> gattServices;


    public BleHostingManager(
        ILogger<IBleHostingManager> logger,
        IEnumerable<BleGattService> gattServices
    )
    {
        this.logger = logger;
        this.gattServices = gattServices;
    }


    public async Task AttachRegisteredServices()
    {
        (await this.RequestAccess()).Assert();


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
                        // TODO: what about just returning the bytes?
                        cb.SetRead(request =>
                        {
                            try
                            {
                                // TODO: this does not allow for async at the moment
                                return ReadResult.Success(new byte[] { 0x0 });
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError("Error executing BleGattService read request", ex);
                                return ReadResult.Error(GattState.Failure);
                            }
                        }, attribute.Secure);
                    }

                    var write = st.GetMethod(nameof(BleGattService.OnWrite))!.DeclaringType != typeof(BleGattService);
                    if (write)
                    {
                        var options = WriteOptions.Write; // TODO: without response?
                        if (attribute.Secure)
                            options |= WriteOptions.EncryptionRequired;

                        cb.SetWrite(request =>
                        {
                            // TODO: why bother returning state?  Maybe allow a custom control?
                            try
                            {
                                // TODO: this does not allow for async at the moment
                                return GattState.Success;
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError("Error executing BleGattService write request", ex);
                                return GattState.Failure;
                            }
                        }, options);
                    }

                    var notify = st.GetMethod(nameof(BleGattService.OnSubscriptionChanged))!.DeclaringType != typeof(BleGattService);
                    if (notify)
                    {
                        // TODO: indicate
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
                                // TODO: log for user
                                this.logger.LogError("Error executing BleGattService notification subscription", ex);
                            }
                        });
                    }
                });

            });
        }
    }


    public void DetachRegisteredServices()
    {
        // TODO: I should keep a registration cache instead of repeated reflection - this isn't going to be a huge perf boost since we aren't doing a lot here
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

