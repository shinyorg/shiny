namespace Shiny.BluetoothLE;


public class BleOperationException : BleException
{
    public BleOperationException(string message, int gattStatusCode) : base(message)
    {
        this.GattStatusCode = gattStatusCode;
    }
    
    
    public int GattStatusCode { get; }
}