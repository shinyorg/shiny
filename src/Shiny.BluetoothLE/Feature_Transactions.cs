using System;
using System.Reactive;

namespace Shiny.BluetoothLE;

/// <summary>
/// Lifecycle states of a GATT reliable-write transaction.
/// </summary>
public enum TransactionState
{
    /// <summary>The transaction is open and queued writes are being staged.</summary>
    Active,

    /// <summary>A commit has been requested and the platform is verifying staged writes.</summary>
    Committing,

    /// <summary>All staged writes were verified and committed by the peripheral.</summary>
    Committed,

    /// <summary>The transaction was aborted and no staged writes were applied.</summary>
    Aborted
}

/// <summary>
/// Thrown when a GATT reliable-write transaction cannot be started, committed, or aborted.
/// </summary>
public class GattReliableWriteTransactionException : Exception
{
    /// <summary>
    /// Creates a new instance with the supplied message.
    /// </summary>
    /// <param name="msg">The error description.</param>
    public GattReliableWriteTransactionException(string msg) : base(msg) { }
}

/// <summary>
/// A scoped GATT reliable-write transaction. Staged writes are verified by the peripheral on commit
/// and rolled back if any verification fails.
/// </summary>
public interface IGattReliableWriteTransaction : IDisposable
{
    /// <summary>
    /// Gets the current transaction state.
    /// </summary>
    TransactionState Status { get; }

    /// <summary>
    /// Stages a characteristic write to be verified at commit time.
    /// </summary>
    /// <param name="peripheral">The target peripheral.</param>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <param name="data">The bytes to stage.</param>
    /// <returns>A single-value observable for the staged write.</returns>
    IObservable<BleCharacteristicResult> Write(IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data);

    /// <summary>
    /// Commits all staged writes atomically.
    /// </summary>
    /// <returns>An observable that completes when the commit is acknowledged.</returns>
    IObservable<Unit> Commit();

    /// <summary>
    /// Aborts the transaction, discarding all staged writes.
    /// </summary>
    void Abort();
}

/// <summary>
/// Optional peripheral capability indicating support for GATT reliable-write transactions.
/// </summary>
public interface ICanDoTransactions : IPeripheral
{
    /// <summary>
    /// Opens a new reliable-write transaction on this peripheral.
    /// </summary>
    /// <returns>The transaction instance. Dispose to release platform resources.</returns>
    IGattReliableWriteTransaction BeginReliableWriteTransaction();
}


/// <summary>
/// Helpers for working with the optional <see cref="ICanDoTransactions"/> capability.
/// </summary>
public static class Feature_Transactions
{
    /// <summary>
    /// Begins a reliable-write transaction if supported by the platform, otherwise returns null.
    /// </summary>
    /// <param name="peripheral">The peripheral to query.</param>
    /// <returns>The transaction, or null if unsupported.</returns>
    public static IGattReliableWriteTransaction? TryBeginTransaction(this IPeripheral peripheral)
    {
        if (peripheral is ICanDoTransactions trans)
            return trans.BeginReliableWriteTransaction();

        return null;
    }

    /// <summary>
    /// Returns true when reliable-write transactions are available on the current platform.
    /// </summary>
    /// <param name="peripheral">The peripheral to test.</param>
    /// <returns>True when transactions are supported; otherwise false.</returns>
    public static bool IsReliableTransactionsAvailable(this IPeripheral peripheral) => peripheral is ICanDoTransactions;
}
