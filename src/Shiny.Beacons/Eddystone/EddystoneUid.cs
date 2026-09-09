using System;

namespace Shiny.Beacons;


/// <summary>
/// The 16-byte Eddystone-UID identity: a 10-byte namespace naming the deployment and a 6-byte
/// instance naming the individual beacon within it.
/// </summary>
public readonly record struct EddystoneUid
{
    /// <summary>The length in bytes of the namespace portion.</summary>
    public const int NamespaceLength = 10;

    /// <summary>The length in bytes of the instance portion.</summary>
    public const int InstanceLength = 6;

    readonly byte[]? bytes;


    /// <summary>
    /// Creates an identity from its two raw parts.
    /// </summary>
    /// <param name="namespaceId">Exactly 10 bytes.</param>
    /// <param name="instanceId">Exactly 6 bytes.</param>
    public EddystoneUid(ReadOnlySpan<byte> namespaceId, ReadOnlySpan<byte> instanceId)
    {
        if (namespaceId.Length != NamespaceLength)
            throw new ArgumentException($"An Eddystone namespace is {NamespaceLength} bytes", nameof(namespaceId));

        if (instanceId.Length != InstanceLength)
            throw new ArgumentException($"An Eddystone instance is {InstanceLength} bytes", nameof(instanceId));

        this.bytes = new byte[NamespaceLength + InstanceLength];
        namespaceId.CopyTo(this.bytes);
        instanceId.CopyTo(this.bytes.AsSpan(NamespaceLength));
    }


    /// <summary>
    /// Parses an identity from its two hex-encoded parts.
    /// </summary>
    /// <param name="namespaceId">20 hex characters.</param>
    /// <param name="instanceId">12 hex characters.</param>
    public static EddystoneUid Parse(string namespaceId, string instanceId)
        => new(Convert.FromHexString(namespaceId), Convert.FromHexString(instanceId));


    /// <summary>The 10-byte namespace as 20 uppercase hex characters.</summary>
    public string Namespace => Convert.ToHexString(this.NamespaceBytes);

    /// <summary>The 6-byte instance as 12 uppercase hex characters.</summary>
    public string Instance => Convert.ToHexString(this.InstanceBytes);

    /// <summary>The raw 10-byte namespace.</summary>
    public ReadOnlySpan<byte> NamespaceBytes => this.Raw.Slice(0, NamespaceLength);

    /// <summary>The raw 6-byte instance.</summary>
    public ReadOnlySpan<byte> InstanceBytes => this.Raw.Slice(NamespaceLength, InstanceLength);

    /// <summary>The full 16 bytes, namespace first.</summary>
    public ReadOnlySpan<byte> Raw => this.bytes ?? Empty;

    static readonly byte[] Empty = new byte[NamespaceLength + InstanceLength];


    /// <inheritdoc />
    public bool Equals(EddystoneUid other) => this.Raw.SequenceEqual(other.Raw);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(this.Raw);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => $"{this.Namespace}:{this.Instance}";
}
