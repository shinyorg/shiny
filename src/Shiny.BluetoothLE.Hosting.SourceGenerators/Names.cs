namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


static class Names
{
    public const string Namespace = "Shiny.BluetoothLE.Hosting";

    public const string BleServiceAttribute = Namespace + ".BleServiceAttribute";
    public const string ReadCharacteristicAttribute = Namespace + ".ReadCharacteristicAttribute";
    public const string WriteCharacteristicAttribute = Namespace + ".WriteCharacteristicAttribute";
    public const string NotifyCharacteristicAttribute = Namespace + ".NotifyCharacteristicAttribute";
    public const string RequestResponseCharacteristicAttribute = Namespace + ".RequestResponseCharacteristicAttribute";
    public const string L2CapServiceAttribute = Namespace + ".L2CapServiceAttribute";
    public const string OnChannelOpenedAttribute = Namespace + ".OnChannelOpenedAttribute";

    public const string ReadRequest = "global::" + Namespace + ".ReadRequest";
    public const string WriteRequest = "global::" + Namespace + ".WriteRequest";
    public const string Peripheral = "global::" + Namespace + ".IPeripheral";
    public const string GattResult = "global::" + Namespace + ".GattResult";
    public const string GattState = "global::" + Namespace + ".GattState";
    public const string GattCharacteristic = "global::" + Namespace + ".IGattCharacteristic";
    public const string GattService = "global::" + Namespace + ".IGattService";
    public const string GattServiceBuilder = "global::" + Namespace + ".IGattServiceBuilder";
    public const string CharacteristicSubscription = "global::" + Namespace + ".CharacteristicSubscription";
    public const string BleSubscription = "global::" + Namespace + ".BleSubscription";
    public const string BleServiceContext = "global::" + Namespace + ".BleServiceContext";
    public const string BleContextStore = "global::" + Namespace + ".BleContextStore";
    public const string BleHostingRuntime = "global::" + Namespace + ".BleHostingRuntime";
    public const string BleL2CapContext = "global::" + Namespace + ".BleL2CapContext";
    public const string BleHostedServiceSession = "global::" + Namespace + ".BleHostedServiceSession";
    public const string BleHostingManager = "global::" + Namespace + ".IBleHostingManager";
    public const string AdvertisementOptions = "global::" + Namespace + ".AdvertisementOptions";
    public const string WriteOptions = "global::" + Namespace + ".WriteOptions";
    public const string NotificationOptions = "global::" + Namespace + ".NotificationOptions";
    public const string L2CapInstance = "global::" + Namespace + ".L2CapInstance";

    public const string L2CapChannel = "global::Shiny.BluetoothLE.L2CapChannel";

    public const string CancellationToken = "global::System.Threading.CancellationToken";
    public const string Task = "global::System.Threading.Tasks.Task";
    public const string ValueTask = "global::System.Threading.Tasks.ValueTask";
    public const string Exception = "global::System.Exception";
    public const string OperationCanceledException = "global::System.OperationCanceledException";
    public const string ServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string ServiceProvider = "global::System.IServiceProvider";

    /// <summary>Field holding the token cancelled when the hosted service is torn down.</summary>
    public const string HostTokenField = "__bleHostCts";

    public const string GeneratorName = "Shiny.BluetoothLE.Hosting.SourceGenerators";
    public const string GeneratorVersion = "0.1.0.0";
}
