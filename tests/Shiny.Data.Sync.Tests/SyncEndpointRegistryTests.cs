using System;
using Shiny.Data.Sync.Infrastructure;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class SyncEndpointRegistryTests
{
    [Fact]
    public void RegisterEndpoint_StoresByKeyAndType()
    {
        var reg = new SyncEndpointRegistry();
        reg.RegisterEndpoint<SampleEntity>("https://api/test");

        Assert.NotNull(reg.Get<SampleEntity>());
        Assert.NotNull(reg.Get(typeof(SampleEntity)));
        Assert.NotNull(reg.Get(typeof(SampleEntity).FullName!));
        Assert.Equal("https://api/test", reg.Get<SampleEntity>()!.Url);
    }


    [Fact]
    public void RegisterEndpoint_AppliesConfigureCallback()
    {
        var reg = new SyncEndpointRegistry();
        reg.RegisterEndpoint<SampleEntity>("https://api/test", ep =>
        {
            ep.MaxAttempts = 9;
            ep.Direction = SyncDirection.PushOnly;
            ep.Batch = true;
        });

        var ep = reg.Get<SampleEntity>()!;
        Assert.Equal(9, ep.MaxAttempts);
        Assert.Equal(SyncDirection.PushOnly, ep.Direction);
        Assert.True(ep.Batch);
    }


    [Fact]
    public void Defaults_Match_DocumentedValues()
    {
        var reg = new SyncEndpointRegistry();
        reg.RegisterEndpoint<SampleEntity>("https://api/test");

        var ep = reg.Get<SampleEntity>()!;
        Assert.Equal(SyncDirection.Both, ep.Direction);
        Assert.Equal(5, ep.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), ep.RetryBaseDelay);
        Assert.Equal("since", ep.CursorParameter);
        Assert.Equal("since", ep.TombstoneCursorParameter);
        Assert.Equal(ConflictPolicy.AskDelegate, ep.DefaultConflictPolicy);
        Assert.True(ep.UseMeteredConnection);
        Assert.False(ep.Batch);
        Assert.Null(ep.MinPullInterval);
        Assert.Null(ep.PullUrl);
        Assert.Null(ep.BatchUrl);
        Assert.Null(ep.TombstoneUrl);
        Assert.Null(ep.SoftDeletePredicate);
        Assert.Null(ep.ExpiryPredicate);
    }


    [Fact]
    public void DuplicateRegistration_Throws()
    {
        var reg = new SyncEndpointRegistry();
        reg.RegisterEndpoint<SampleEntity>("https://api/test");

        Assert.Throws<InvalidOperationException>(() => reg.RegisterEndpoint<SampleEntity>("https://other"));
    }


    [Fact]
    public void CapturedSerializer_HandlesEntity()
    {
        var reg = new SyncEndpointRegistry();
        reg.RegisterEndpoint<SampleEntity>("https://api/test");
        var ep = reg.Get<SampleEntity>()!;

        var json = ep.SerializeEntity(new SampleEntity("id-1", "Alice"));

        Assert.Contains("id-1", json);
        Assert.Contains("Alice", json);

        var roundTripped = (SampleEntity?)ep.DeserializeEntity(json);
        Assert.NotNull(roundTripped);
        Assert.Equal("id-1", roundTripped!.Identifier);
        Assert.Equal("Alice", roundTripped.Name);
    }


    [Fact]
    public void GetUnregistered_ReturnsNull()
    {
        var reg = new SyncEndpointRegistry();
        Assert.Null(reg.Get<OtherEntity>());
        Assert.Null(reg.Get("doesnt.exist"));
    }


    [Fact]
    public void All_EnumeratesEveryEndpoint()
    {
        var reg = new SyncEndpointRegistry();
        reg.RegisterEndpoint<SampleEntity>("https://api/sample");
        reg.RegisterEndpoint<OtherEntity>("https://api/other");

        Assert.Equal(2, reg.All.Count);
    }
}
