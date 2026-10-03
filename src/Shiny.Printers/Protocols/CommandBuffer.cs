using System.Collections.Generic;

namespace Shiny.Printers.Protocols;


/// <summary>A tiny growable byte sink with convenience helpers, used by protocol encoders.</summary>
public sealed class CommandBuffer
{
    readonly List<byte> bytes = new(256);

    /// <summary>Appends one byte.</summary>
    public CommandBuffer Write(byte b)
    {
        this.bytes.Add(b);
        return this;
    }

    /// <summary>Appends a sequence of bytes.</summary>
    public CommandBuffer Write(params byte[] data)
    {
        this.bytes.AddRange(data);
        return this;
    }

    /// <summary>Appends a span of bytes.</summary>
    public CommandBuffer Write(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            this.bytes.Add(b);
        return this;
    }

    /// <summary>Current length in bytes.</summary>
    public int Length => this.bytes.Count;

    /// <summary>Returns the accumulated bytes as a new array.</summary>
    public byte[] ToArray() => this.bytes.ToArray();
}
