using Shiny.BluetoothLE.Managed;

namespace Shiny.BluetoothLE;


public static class ManagedExtensions
{
    public static IManagedScan CreateManagedScanner(this IBleManager bleManager)
        => new ManagedScan(bleManager);
}
