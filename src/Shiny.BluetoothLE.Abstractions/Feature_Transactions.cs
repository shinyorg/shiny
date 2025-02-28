using System;
using System.Reactive;

namespace Shiny.BluetoothLE;

public enum TransactionState
{
    Active,
    Committing,
    Committed,
    Aborted
}

public class GattReliableWriteTransactionException : Exception
{
    public GattReliableWriteTransactionException(string msg) : base(msg) { }
}

public interface IGattReliableWriteTransaction : IDisposable
{
    TransactionState Status { get; }
    IObservable<BleCharacteristicResult> Write(IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data);
    IObservable<Unit> Commit();
    void Abort();
}

public interface ICanDoTransactions : IPeripheral
{
    IGattReliableWriteTransaction BeginReliableWriteTransaction();
}


public static class Feature_Transactions
{
    public static IGattReliableWriteTransaction? TryBeginTransaction(this IPeripheral peripheral)
    {
        if (peripheral is ICanDoTransactions trans)
            return trans.BeginReliableWriteTransaction();

        return null;
    }

    public static bool IsReliableTransactionsAvailable(this IPeripheral peripheral) => peripheral is ICanDoTransactions;
}
