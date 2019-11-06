using System;


namespace Shiny.BluetoothLE.Central
{
    public interface ICanDoTransactions
    {
        IGattReliableWriteTransaction BeginReliableWriteTransaction();
    }


    public static class Feature_Transactions
    {
        public static IGattReliableWriteTransaction BeginTransaction(this IPeripheral peripheral)
            => ((ICanDoTransactions)peripheral).BeginReliableWriteTransaction();


        public static IGattReliableWriteTransaction TryBeginTransaction(this IPeripheral peripheral)
        {
            if (peripheral is ICanDoTransactions trans)
                return trans.BeginReliableWriteTransaction();

            return null;
        }

        public static bool IsReliableTransactionsAvailable(this IPeripheral peripheral) => peripheral is ICanDoTransactions;
    }
}
