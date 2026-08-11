using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// An immutable array with structural equality, so models carrying collections still cache
/// correctly on the incremental pipeline. <see cref="ImmutableArray{T}"/> compares by reference.
/// </summary>
readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    readonly ImmutableArray<T> items;


    public EquatableArray(ImmutableArray<T> items) => this.items = items;

    public EquatableArray(IEnumerable<T> items) => this.items = ImmutableArray.CreateRange(items);


    public int Count => this.items.IsDefault ? 0 : this.items.Length;

    public T this[int index] => this.items[index];


    public bool Equals(EquatableArray<T> other)
    {
        if (this.Count != other.Count)
            return false;

        for (var i = 0; i < this.Count; i++)
        {
            if (!this.items[i].Equals(other.items[i]))
                return false;
        }
        return true;
    }


    public override bool Equals(object? obj) => obj is EquatableArray<T> other && this.Equals(other);


    public override int GetHashCode()
    {
        var hash = 17;
        for (var i = 0; i < this.Count; i++)
            hash = unchecked((hash * 31) + this.items[i].GetHashCode());

        return hash;
    }


    public IEnumerator<T> GetEnumerator()
    {
        if (this.items.IsDefault)
            yield break;

        foreach (var item in this.items)
            yield return item;
    }


    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}


static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source)
        where T : IEquatable<T>
        => new(source);
}
