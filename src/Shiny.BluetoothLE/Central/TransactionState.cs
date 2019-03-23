using System;


namespace Shiny.BluetoothLE.Central
{
    public enum TransactionState
    {
        Active,
        Committing,
        Committed,
        Aborted
    }
}
