using Shiny.Bluetooth.Bluez;
using Xunit;

namespace Shiny.Bluetooth.Tests;


public class BluezTests
{
    [Fact]
    public void DevicePath_UsesBluezNaming()
        => Assert.Equal("/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF", BluezConstants.DevicePath("/org/bluez/hci0", "aa:bb:cc:dd:ee:ff"));


    [Fact]
    public void AddressFromPath_RoundTrips()
        => Assert.Equal("AA:BB:CC:DD:EE:FF", LinuxBluetoothManager.AddressFromPath("/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF"));


    [Fact]
    public void AddressFromPath_RejectsNonDevicePaths()
        => Assert.Throws<ArgumentException>(() => LinuxBluetoothManager.AddressFromPath("/org/bluez/hci0"));
}
