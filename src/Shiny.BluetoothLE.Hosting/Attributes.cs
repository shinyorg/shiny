using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Marks a partial class as a hosted GATT service. The source generator emits the
/// <see cref="IBleHostingManager.AddService(string, bool, Action{IGattServiceBuilder})"/> plumbing
/// for every characteristic handler declared on the class.
/// </summary>
/// <remarks>
/// More than one class may declare the same <see cref="Uuid"/> - the generator merges them into a
/// single service registration. Declaring the same characteristic UUID twice inside a merge group
/// is a compile error (SBH010).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BleServiceAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="uuid">The service UUID (16, 32, or 128 bit form).</param>
    public BleServiceAttribute(string uuid) => this.Uuid = uuid;


    /// <summary>
    /// Gets the service UUID.
    /// </summary>
    public string Uuid { get; }

    /// <summary>
    /// Whether this is a primary service. Defaults to true.
    /// </summary>
    public bool Primary { get; set; } = true;

    /// <summary>
    /// Includes this service UUID in the generated <c>StartBleHostedAdvertising</c> call. Defaults to false.
    /// </summary>
    public bool Advertise { get; set; }

    /// <summary>
    /// Names the generated registration extension method (<c>Add{Name}</c>). Defaults to the class
    /// name. When several classes merge into one service UUID, the first non-null name wins.
    /// </summary>
    public string? Name { get; set; }
}


/// <summary>
/// Declares the method as the read handler for a characteristic.
/// </summary>
/// <remarks>
/// The method may return <c>byte[]</c> or <c>GattResult</c> (optionally wrapped in
/// <c>Task</c>/<c>ValueTask</c>) and may take any subset of <c>ReadRequest</c>, the service's
/// generated context type, <c>IPeripheral</c>, <c>int</c> (the read offset), and
/// <c>CancellationToken</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ReadCharacteristicAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="uuid">The characteristic UUID.</param>
    public ReadCharacteristicAttribute(string uuid) => this.Uuid = uuid;


    /// <summary>
    /// Gets the characteristic UUID.
    /// </summary>
    public string Uuid { get; }

    /// <summary>
    /// Requires an encrypted link before the read is served. Defaults to false.
    /// </summary>
    public bool Encrypted { get; set; }
}


/// <summary>
/// Declares the method as the write handler for a characteristic.
/// </summary>
/// <remarks>
/// The method may return <c>void</c>, <c>GattState</c>, <c>Task</c>/<c>ValueTask</c>, or
/// <c>Task&lt;GattState&gt;</c>/<c>ValueTask&lt;GattState&gt;</c>, and may take any subset of
/// <c>byte[]</c> (the written data), <c>WriteRequest</c>, the service's generated context type,
/// <c>IPeripheral</c>, <c>int</c> (the write offset), and <c>CancellationToken</c>.
/// When the handler returns no status, the generator responds <c>GattState.Success</c> - and
/// <c>GattState.Failure</c> if it threw - but only when <c>WriteRequest.IsReplyNeeded</c> is set.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class WriteCharacteristicAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="uuid">The characteristic UUID.</param>
    public WriteCharacteristicAttribute(string uuid) => this.Uuid = uuid;


    /// <summary>
    /// Gets the characteristic UUID.
    /// </summary>
    public string Uuid { get; }

    /// <summary>
    /// Accepts unacknowledged writes without response. Defaults to false.
    /// </summary>
    public bool WriteWithoutResponse { get; set; }

    /// <summary>
    /// Accepts authenticated signed writes. Defaults to false.
    /// </summary>
    public bool AuthenticatedSignedWrites { get; set; }

    /// <summary>
    /// Requires an encrypted link before writes are accepted. Defaults to false.
    /// </summary>
    public bool EncryptionRequired { get; set; }

    /// <summary>
    /// Suppresses the generated <c>Respond</c> call so the handler can answer the central itself.
    /// Requires a <c>WriteRequest</c> parameter and a handler that returns no <c>GattState</c>.
    /// Defaults to false.
    /// </summary>
    /// <remarks>
    /// Responding twice to one request is undefined across platforms, which is why this is opt-in
    /// rather than inferred from the signature.
    /// </remarks>
    public bool ManualRespond { get; set; }
}


/// <summary>
/// Declares notification (or indication) support for a characteristic. The decorated method is an
/// optional subscribe/unsubscribe hook; the push API is generated either way.
/// </summary>
/// <remarks>
/// For a characteristic named <c>HeartRate</c>, the generator emits
/// <c>NotifyHeartRate(byte[], params IPeripheral[])</c>, <c>HeartRateSubscribers</c>, and
/// <c>HasHeartRateSubscribers</c> on the partial class.
/// <para>
/// Place it on the service class instead of a method when you want the push API without a
/// subscription hook - the common read-plus-notify shape. <see cref="Name"/> is required there,
/// since there is no method name to derive it from.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class NotifyCharacteristicAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="uuid">The characteristic UUID.</param>
    public NotifyCharacteristicAttribute(string uuid) => this.Uuid = uuid;


    /// <summary>
    /// Gets the characteristic UUID.
    /// </summary>
    public string Uuid { get; }

    /// <summary>
    /// Sends acknowledged indications instead of notifications. Defaults to false.
    /// </summary>
    public bool Indicate { get; set; }

    /// <summary>
    /// Requires an encrypted link before notifying. Defaults to false.
    /// </summary>
    public bool EncryptionRequired { get; set; }

    /// <summary>
    /// Names the generated push members. Defaults to the method name with a leading <c>On</c> and a
    /// trailing <c>Async</c> stripped.
    /// </summary>
    public string? Name { get; set; }
}


/// <summary>
/// Declares a write characteristic whose handler result is pushed back to the writing central as a
/// notification. The characteristic is registered with both write and notify support.
/// </summary>
/// <remarks>
/// A GATT write response cannot carry a payload, so the reply travels as a notification addressed to
/// the central that issued the write. That central must be subscribed before writing, otherwise the
/// result is dropped (see the generated <c>OnResponseDropped</c> hook).
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class RequestResponseCharacteristicAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="uuid">The characteristic UUID.</param>
    public RequestResponseCharacteristicAttribute(string uuid) => this.Uuid = uuid;


    /// <summary>
    /// Gets the characteristic UUID.
    /// </summary>
    public string Uuid { get; }

    /// <summary>
    /// Sends the reply as an acknowledged indication instead of a notification. Defaults to false.
    /// </summary>
    public bool Indicate { get; set; }

    /// <summary>
    /// Requires an encrypted link. Defaults to false.
    /// </summary>
    public bool EncryptionRequired { get; set; }

    /// <summary>
    /// Names the generated push members. Defaults to the method name with a leading <c>On</c> and a
    /// trailing <c>Async</c> stripped.
    /// </summary>
    public string? Name { get; set; }
}


/// <summary>
/// Marks a partial class as an L2CAP CoC listener. The generator owns the listener lifetime and
/// hands each accepted central to the class's <see cref="OnChannelOpenedAttribute"/> handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class L2CapServiceAttribute : Attribute
{
    /// <summary>
    /// Requires an encrypted/authenticated channel (Android API 29+). Defaults to false.
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// GATT service UUID the assigned PSM is published on. Must be a UUID some
    /// <see cref="BleServiceAttribute"/> in the compilation declares, and must be set together with
    /// <see cref="PsmCharacteristic"/>.
    /// </summary>
    public string? PsmService { get; set; }

    /// <summary>
    /// Characteristic UUID that serves the assigned PSM as two little-endian bytes. Centrals have no
    /// other in-band way to learn the PSM.
    /// </summary>
    public string? PsmCharacteristic { get; set; }

    /// <summary>
    /// Names the generated registration extension method (<c>Add{Name}</c>). Defaults to the class name.
    /// </summary>
    public string? Name { get; set; }
}


/// <summary>
/// Declares the method that services each accepted L2CAP channel. Exactly one is required on an
/// <see cref="L2CapServiceAttribute"/> class.
/// </summary>
/// <remarks>
/// The method may take any subset of <c>L2CapChannel</c>, <c>BleL2CapContext</c>, and
/// <c>CancellationToken</c>, and may return <c>void</c>, <c>Task</c>, or <c>ValueTask</c>. The
/// channel is disposed once the handler completes.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnChannelOpenedAttribute : Attribute;
