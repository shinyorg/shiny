using Xunit;
using Shiny.BluetoothLE.AccessorySetup.Infrastructure;

namespace Shiny.BluetoothLE.AccessorySetup.Tests;


public class PickerFlowTests
{
    static readonly SetupAccessory Accessory = new("id", "Sensor", "id", AccessoryAuthorizationState.Authorized);


    [Fact]
    public async Task AddedThenDismissed_ReturnsAccessory()
    {
        var flow = new PickerFlow();
        flow.OnShowCompleted(null);
        flow.OnAccessoryAdded(Accessory);
        Assert.False(flow.IsCompleted);

        flow.OnDismissed();
        Assert.Same(Accessory, await flow.Task);
    }


    [Fact]
    public async Task DismissedWithoutAccessory_ReturnsNull()
    {
        var flow = new PickerFlow();
        flow.OnShowCompleted(null);
        flow.OnDismissed();
        Assert.Null(await flow.Task);
    }


    [Fact]
    public async Task SetupFailedThenDismissed_Throws()
    {
        var flow = new PickerFlow();
        flow.OnSetupFailed(new AccessorySetupException(AccessorySetupErrorCode.Failed, "boom"));
        flow.OnDismissed();

        var ex = await Assert.ThrowsAsync<AccessorySetupException>(() => flow.Task);
        Assert.Equal("boom", ex.Message);
    }


    [Fact]
    public async Task AddedWinsOverRecoveredFailure()
    {
        var flow = new PickerFlow();
        flow.OnSetupFailed(new AccessorySetupException(AccessorySetupErrorCode.Failed, "retry"));
        flow.OnAccessoryAdded(Accessory);
        flow.OnDismissed();
        Assert.Same(Accessory, await flow.Task);
    }


    [Fact]
    public async Task ShowFailure_Throws()
    {
        var flow = new PickerFlow();
        flow.OnShowCompleted(new AccessorySetupException(AccessorySetupErrorCode.PickerAlreadyActive, "busy"));

        var ex = await Assert.ThrowsAsync<AccessorySetupException>(() => flow.Task);
        Assert.Equal(AccessorySetupErrorCode.PickerAlreadyActive, ex.Code);
    }


    [Fact]
    public async Task Cancelled_ReturnsNull()
    {
        var flow = new PickerFlow();
        flow.OnCancelled();
        Assert.Null(await flow.Task);
    }


    [Fact]
    public async Task Invalidated_Throws()
    {
        var flow = new PickerFlow();
        flow.OnShowCompleted(null);
        flow.OnInvalidated();
        await Assert.ThrowsAsync<AccessorySetupException>(() => flow.Task);
    }


    [Fact]
    public async Task CancelToken_Cancels()
    {
        using var cts = new CancellationTokenSource();
        var flow = new PickerFlow();
        cts.Cancel();
        flow.OnCancelToken(cts.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flow.Task);
    }


    [Fact]
    public async Task EventsAfterCompletion_AreIgnored()
    {
        var flow = new PickerFlow();
        flow.OnDismissed();
        flow.OnAccessoryAdded(Accessory);
        flow.OnInvalidated();
        Assert.Null(await flow.Task);
    }
}
