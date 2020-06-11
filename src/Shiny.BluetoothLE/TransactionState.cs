using System;


namespace Shiny.BluetoothLE
{
    public enum TransactionState
    {
        Active,
        Committing,
        Committed,
        Aborted
    }
}
