namespace Shiny.Tests.BluetoothLE;

public class BleConfiguration
{
    public TimeSpan DeviceScanTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public string PeripheralName { get; set; }

    public const string ServiceUuid = "8c927255-fdec-4605-957b-57b6450779c0";    
    public const string ReadCharacteristicUuid = "8c927255-fdec-4605-957b-57b6450779c1";    
    public const string WriteCharacteristicUuid = "8c927255-fdec-4605-957b-57b6450779c2";
    public const string NotifyCharacteristicUuid = "8c927255-fdec-4605-957b-57b6450779c3";


    public const string SecondaryServiceUuid = "9c927255-fdec-4605-957b-57b6450779c0";
    public const string SecondaryCharacteristicUuid1 = "9c927255-fdec-4605-957b-57b6450779c1";
    public const string SecondaryCharacteristicUuid2 = "9c927255-fdec-4605-957b-57b6450779c2";

}
