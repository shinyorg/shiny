using System.Text;
using Shiny.BluetoothLE.Hosting;
// Shiny.BluetoothLE (a global using here) also defines a DeviceInfo
using DeviceInfo = Microsoft.Maui.Devices.DeviceInfo;

namespace Sample.Shared.Maui.BleHosting;


/// <summary>
/// A second class declaring the <em>same</em> service UUID as <see cref="SampleGeneratedGattService"/>.
/// The generator merges both into a single <c>AddService</c> call - BleHostingManager keys services
/// by UUID, so two registrations would throw. Declaring the same characteristic UUID in both classes
/// is a compile error (SBH010).
/// </summary>
[BleService(SampleGattUuids.Service)]
public partial class SampleDeviceInfoService
{
    [ReadCharacteristic(SampleGattUuids.Model)]
    byte[] ReadModel() => Encoding.UTF8.GetBytes($"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}");
}
