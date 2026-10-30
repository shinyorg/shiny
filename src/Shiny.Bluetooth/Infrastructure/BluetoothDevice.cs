namespace Shiny.Bluetooth.Infrastructure;


/// <summary>
/// What a platform manager does for its devices - <see cref="IBluetoothDevice"/>'s operations live with
/// the manager that owns the native adapter, and the device delegates to it.
/// </summary>
internal interface IBluetoothDeviceOperations
{
    Task<bool> Pair(BluetoothDevice device, CancellationToken ct);
    Task<BluetoothConnection> Connect(BluetoothDevice device, Guid serviceUuid, bool secure, CancellationToken ct);
}


/// <summary>
/// The one <see cref="IBluetoothDevice"/> implementation every platform hands out.
/// </summary>
internal sealed class BluetoothDevice : IBluetoothDevice, IEquatable<BluetoothDevice>
{
    readonly IBluetoothDeviceOperations operations;


    public BluetoothDevice(IBluetoothDeviceOperations operations, string address)
    {
        this.operations = operations;
        this.Address = BluetoothAddress.Normalize(address);
    }


    public string Address { get; }
    public string? Name { get; init; }
    public bool IsPaired { get; private set; }
    public uint? ClassOfDevice { get; init; }

    public BluetoothMajorDeviceClass? MajorDeviceClass => this.ClassOfDevice is { } cod
        ? (BluetoothMajorDeviceClass)((cod >> 8) & 0x1F)
        : null;


    /// <summary>Set by the platform when reading the device; kept current by <see cref="Pair"/>.</summary>
    public bool Paired
    {
        init => this.IsPaired = value;
    }


    public async Task<bool> Pair(CancellationToken ct = default)
    {
        var paired = await this.operations.Pair(this, ct).ConfigureAwait(false);
        if (paired)
            this.IsPaired = true;

        return paired;
    }


    public Task<BluetoothConnection> Connect(Guid serviceUuid, bool secure = true, CancellationToken ct = default)
        => this.operations.Connect(this, serviceUuid, secure, ct);


    public bool Equals(BluetoothDevice? other) => other != null && this.Address == other.Address;
    public override bool Equals(object? obj) => obj is IBluetoothDevice other && this.Address == other.Address;
    public override int GetHashCode() => this.Address.GetHashCode();
    public override string ToString() => this.Name == null ? this.Address : $"{this.Name} ({this.Address})";
}
