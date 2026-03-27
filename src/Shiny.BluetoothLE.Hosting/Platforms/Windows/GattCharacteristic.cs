using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Shiny.BluetoothLE.Hosting;


public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder, IDisposable
{
    GattLocalCharacteristic? native;
    GattCharacteristicProperties properties = 0;
    GattProtectionLevel readProtection = GattProtectionLevel.Plain;
    GattProtectionLevel writeProtection = GattProtectionLevel.Plain;

    Func<ReadRequest, Task<GattResult>>? onRead;
    Func<WriteRequest, Task>? onWrite;
    Func<CharacteristicSubscription, Task>? onSubscribe;

    readonly Dictionary<string, Peripheral> subscribers = new();


    public GattCharacteristic(string uuid) => this.Uuid = uuid;


    public string Uuid { get; }
    public CharacteristicProperties Properties => (CharacteristicProperties)(int)this.properties;
    public IReadOnlyList<IPeripheral> SubscribedCentrals => this.subscribers.Values.Cast<IPeripheral>().ToList();


    public IGattCharacteristicBuilder SetRead(Func<ReadRequest, Task<GattResult>> request, bool encrypted = false)
    {
        this.onRead = request;
        this.properties |= GattCharacteristicProperties.Read;
        if (encrypted)
            this.readProtection = GattProtectionLevel.EncryptionRequired;

        return this;
    }


    public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, Task> request, WriteOptions options = WriteOptions.Write)
    {
        this.onWrite = request;

        var enc = options.HasFlag(WriteOptions.EncryptionRequired);
        var auth = options.HasFlag(WriteOptions.AuthenticatedSignedWrites);

        if (enc && auth)
            this.writeProtection = GattProtectionLevel.EncryptionAndAuthenticationRequired;
        else if (enc)
            this.writeProtection = GattProtectionLevel.EncryptionRequired;
        else if (auth)
            this.writeProtection = GattProtectionLevel.AuthenticationRequired;

        if (options.HasFlag(WriteOptions.Write))
            this.properties |= GattCharacteristicProperties.Write;

        if (options.HasFlag(WriteOptions.WriteWithoutResponse))
            this.properties |= GattCharacteristicProperties.WriteWithoutResponse;

        return this;
    }


    public IGattCharacteristicBuilder SetNotification(Func<CharacteristicSubscription, Task>? onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
    {
        this.onSubscribe = onSubscribe;

        if (options.HasFlag(NotificationOptions.Indicate))
            this.properties |= GattCharacteristicProperties.Indicate;

        if (options.HasFlag(NotificationOptions.Notify))
            this.properties |= GattCharacteristicProperties.Notify;

        return this;
    }


    public async Task Build(GattServiceProviderResult native)
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = this.properties,
            ReadProtectionLevel = this.readProtection,
            WriteProtectionLevel = this.writeProtection
        };

        var uuid = UuidHelper.ToUuid(this.Uuid);
        var result = await native.ServiceProvider.Service.CreateCharacteristicAsync(uuid, parameters);
        this.native = result.Characteristic;

        if (this.onRead != null)
            this.native.ReadRequested += this.OnReadRequested;
        if (this.onWrite != null)
            this.native.WriteRequested += this.OnWriteRequested;
        if (this.onSubscribe != null)
            this.native.SubscribedClientsChanged += this.OnSubscribedClientsChanged;
    }


    public async Task Notify(byte[] data, params IPeripheral[] centrals)
    {
        if (this.native == null)
            throw new InvalidOperationException("Characteristic has not been built");

        var writer = new DataWriter();
        writer.WriteBytes(data);
        var buffer = writer.DetachBuffer();

        if (centrals.Length == 0)
        {
            await this.native.NotifyValueAsync(buffer);
        }
        else
        {
            foreach (var central in centrals)
            {
                var client = this.native.SubscribedClients.FirstOrDefault(
                    c => c.Session.DeviceId.Id == central.Uuid
                );
                if (client != null)
                    await this.native.NotifyValueAsync(buffer, client);
            }
        }
    }


    public void Dispose()
    {
        if (this.native != null)
        {
            this.native.ReadRequested -= this.OnReadRequested;
            this.native.WriteRequested -= this.OnWriteRequested;
            this.native.SubscribedClientsChanged -= this.OnSubscribedClientsChanged;
        }
    }


    async void OnReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        var peripheral = new Peripheral(args.Session);
        var readRequest = new ReadRequest(this, peripheral, (int)request.Offset);

        var result = await this.onRead!(readRequest);

        if (result.Status == GattState.Success && result.Data != null)
        {
            var writer = new DataWriter();
            writer.WriteBytes(result.Data);
            request.RespondWithValue(writer.DetachBuffer());
        }
        else
        {
            request.RespondWithProtocolError((byte)result.Status);
        }
    }


    async void OnWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        var peripheral = new Peripheral(args.Session);

        var reader = DataReader.FromBuffer(request.Value);
        var data = new byte[request.Value.Length];
        reader.ReadBytes(data);

        var isReplyNeeded = request.Option == GattWriteOption.WriteWithResponse;
        var responded = false;

        var writeRequest = new WriteRequest(
            this,
            peripheral,
            data,
            (int)request.Offset,
            isReplyNeeded,
            state =>
            {
                responded = true;
                if (isReplyNeeded)
                {
                    if (state == GattState.Success)
                        request.Respond();
                    else
                        request.RespondWithProtocolError((byte)state);
                }
            }
        );

        await this.onWrite!(writeRequest);

        if (!responded && isReplyNeeded)
            request.Respond();
    }


    async void OnSubscribedClientsChanged(GattLocalCharacteristic sender, object args)
    {
        var currentIds = new HashSet<string>();
        foreach (var client in sender.SubscribedClients)
        {
            var deviceId = client.Session.DeviceId.Id;
            currentIds.Add(deviceId);

            if (!this.subscribers.ContainsKey(deviceId))
            {
                var peripheral = new Peripheral(client.Session);
                this.subscribers[deviceId] = peripheral;
                await this.onSubscribe!(new CharacteristicSubscription(this, peripheral, true));
            }
        }

        var removedIds = this.subscribers.Keys.Where(k => !currentIds.Contains(k)).ToList();
        foreach (var id in removedIds)
        {
            var peripheral = this.subscribers[id];
            this.subscribers.Remove(id);
            await this.onSubscribe!(new CharacteristicSubscription(this, peripheral, false));
        }
    }
}
