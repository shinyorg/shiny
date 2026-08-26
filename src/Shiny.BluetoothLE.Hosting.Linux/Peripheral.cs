namespace Shiny.BluetoothLE.Hosting;


public class Peripheral : IPeripheral
{
    public Peripheral(string devicePath, string address, int mtu = BleConstants.DefaultPayloadSize)
    {
        this.DevicePath = devicePath;
        this.Uuid = address;
        this.Mtu = mtu;
    }


    /// <summary>
    /// The BlueZ D-Bus object path for the connected device (e.g. /org/bluez/hci0/dev_XX_XX_XX_XX_XX_XX)
    /// </summary>
    public string DevicePath { get; }
    public string Uuid { get; }
    public int Mtu { get; internal set; }
    public object? Context { get; set; }
}
