namespace Shiny.BluetoothLE.Hosting;


public interface IPeripheral
{
    /// <summary>
    /// The connection ID
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// The current MTU
    /// </summary>
    int Mtu { get; }

    /// <summary>
    /// You can set any data you want here
    /// </summary>
    object? Context { get; set; }
}
