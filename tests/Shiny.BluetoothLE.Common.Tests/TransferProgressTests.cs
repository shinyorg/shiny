using System;
using Shiny.BluetoothLE;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


public class TransferProgressTests
{
    // -------- Empty --------

    [Fact]
    public void Empty_HasZeroEverything()
    {
        Assert.Equal(0, TransferProgress.Empty.BytesPerSecond);
        Assert.Equal(0L, TransferProgress.Empty.BytesToTransfer);
        Assert.Equal(0, TransferProgress.Empty.BytesTransferred);
    }


    // -------- Deterministic flag --------

    [Fact]
    public void IsDeterministic_TrueWhenTotalKnown()
    {
        var p = new TransferProgress(0, BytesToTransfer: 100, BytesTransferred: 0);
        Assert.True(p.IsDeterministic);
    }


    [Fact]
    public void IsDeterministic_FalseWhenTotalUnknown()
    {
        var p = new TransferProgress(0, BytesToTransfer: null, BytesTransferred: 0);
        Assert.False(p.IsDeterministic);
    }


    // -------- PercentComplete --------

    [Fact]
    public void PercentComplete_NotDeterministic_ReturnsNegativeOne()
    {
        var p = new TransferProgress(0, null, 50);
        Assert.Equal(-1, p.PercentComplete);
    }


    [Fact]
    public void PercentComplete_HalfDone_IsRoundedToHundredth()
    {
        var p = new TransferProgress(0, 100, 50);
        Assert.Equal(0.5, p.PercentComplete);
    }


    [Fact]
    public void PercentComplete_Complete_IsOne()
    {
        var p = new TransferProgress(0, 100, 100);
        Assert.Equal(1.0, p.PercentComplete);
    }


    [Fact]
    public void PercentComplete_ZeroProgress_IsZero()
    {
        var p = new TransferProgress(0, 100, 0);
        Assert.Equal(0.0, p.PercentComplete);
    }


    [Fact]
    public void PercentComplete_RoundsToTwoDecimals()
    {
        // 1/3 → 0.33
        var p = new TransferProgress(0, 300, 100);
        Assert.Equal(0.33, p.PercentComplete);
    }


    [Fact]
    public void PercentComplete_Cached_OnSecondAccess()
    {
        var p = new TransferProgress(0, 100, 50);
        Assert.Equal(0.5, p.PercentComplete);
        Assert.Equal(0.5, p.PercentComplete);
    }


    // -------- EstimatedTimeRemaining --------

    [Fact]
    public void Eta_NotDeterministic_IsZero()
    {
        var p = new TransferProgress(BytesPerSecond: 100, BytesToTransfer: null, BytesTransferred: 0);
        Assert.Equal(TimeSpan.Zero, p.EstimatedTimeRemaining);
    }


    [Fact]
    public void Eta_NoThroughput_IsZero()
    {
        var p = new TransferProgress(BytesPerSecond: 0, BytesToTransfer: 1000, BytesTransferred: 0);
        Assert.Equal(TimeSpan.Zero, p.EstimatedTimeRemaining);
    }


    [Fact]
    public void Eta_BytesRemainingDividedByThroughput()
    {
        // 500 remaining at 100 B/s → 5 seconds
        var p = new TransferProgress(BytesPerSecond: 100, BytesToTransfer: 1000, BytesTransferred: 500);
        Assert.Equal(TimeSpan.FromSeconds(5), p.EstimatedTimeRemaining);
    }


    [Fact]
    public void Eta_AtCompletion_IsZero()
    {
        var p = new TransferProgress(BytesPerSecond: 100, BytesToTransfer: 1000, BytesTransferred: 1000);
        Assert.Equal(TimeSpan.Zero, p.EstimatedTimeRemaining);
    }
}
