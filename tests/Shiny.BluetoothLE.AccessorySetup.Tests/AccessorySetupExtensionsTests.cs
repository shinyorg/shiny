using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.BluetoothLE.AccessorySetup.Tests;


public class AccessorySetupExtensionsTests
{
    [Fact]
    public void AddAccessorySetup_RegistersNothingOnBaseTarget()
    {
        var services = new ServiceCollection();
        services.AddAccessorySetup();
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IAccessorySetupManager));
    }


    [Fact]
    public void AddAccessorySetup_RegistersDelegate()
    {
        var services = new ServiceCollection();
        services.AddAccessorySetup<TestDelegate>();
        Assert.Contains(services, x => x.ServiceType == typeof(IAccessorySetupDelegate));
    }


    [Fact]
    public async Task Delegate_DefaultsAreNoOps()
    {
        IAccessorySetupDelegate del = new TestDelegate();
        var accessory = new SetupAccessory("id", null, null, AccessoryAuthorizationState.Authorized);
        await del.OnAccessoryAdded(accessory);
        await del.OnAccessoryRemoved(accessory);
        await del.OnAccessoryChanged(accessory);
        await del.OnPresenceChanged(accessory, true);
    }


    [Fact]
    public void GetPeripheral_NoBluetoothIdentity_ReturnsNull()
    {
        var accessory = new SetupAccessory("id", null, null, AccessoryAuthorizationState.Authorized);
        Assert.Null(new NullBleManager().GetPeripheral(accessory));
    }


    class TestDelegate : IAccessorySetupDelegate;
}


public class GetPeripheralLookupTests
{
    [Fact]
    public void GetPeripheral_LooksUpByPeripheralUuid()
    {
        var ble = new NullBleManager();
        ble.GetPeripheral(new SetupAccessory("1", null, "abc", AccessoryAuthorizationState.Authorized));
        Assert.Equal("abc", ble.LastLookup);
    }
}
