using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Holds the per-central <see cref="BleServiceContext"/> instances created by generated code.
/// </summary>
/// <remarks>
/// Keyed off the <see cref="IPeripheral"/> instance through a <see cref="ConditionalWeakTable{TKey,TValue}"/>
/// rather than <see cref="IPeripheral.Context"/>, because that slot is public API an app may already
/// be using, and a weak-table entry dies with the peripheral instead of pinning it. Shiny caches one
/// <see cref="IPeripheral"/> per connected central, so contexts survive across requests from that central.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BleContextStore
{
    static readonly ConditionalWeakTable<IPeripheral, Bag> table = new();


    /// <summary>
    /// Returns the context of type <typeparamref name="TContext"/> for the supplied central, creating
    /// it on first use. Called by generated code.
    /// </summary>
    /// <typeparam name="TContext">The generated context type.</typeparam>
    /// <param name="peripheral">The central to scope the context to.</param>
    /// <param name="factory">Creates the context when one does not exist yet.</param>
    /// <returns>The context instance for this central.</returns>
    public static TContext GetOrAdd<TContext>(IPeripheral peripheral, Func<TContext> factory)
        where TContext : BleServiceContext
    {
        if (peripheral == null) throw new ArgumentNullException(nameof(peripheral));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var bag = table.GetValue(peripheral, static _ => new Bag());
        return (TContext)bag.GetOrAdd(typeof(TContext), factory);
    }


    /// <summary>
    /// Drops every context held for the supplied central. Call when you know a central is gone and
    /// want its state released early - the entry is collected on its own otherwise.
    /// </summary>
    /// <param name="peripheral">The central to forget.</param>
    public static void Remove(IPeripheral peripheral)
    {
        if (peripheral == null) throw new ArgumentNullException(nameof(peripheral));
        table.Remove(peripheral);
    }


    sealed class Bag
    {
        readonly Dictionary<Type, BleServiceContext> contexts = new();


        public BleServiceContext GetOrAdd<TContext>(Type key, Func<TContext> factory)
            where TContext : BleServiceContext
        {
            lock (this.contexts)
            {
                if (this.contexts.TryGetValue(key, out var existing))
                    return existing;

                var created = factory();
                this.contexts.Add(key, created);
                return created;
            }
        }
    }
}
