using System;
using System.Collections.Generic;
using Shiny.Notifications;
using Xunit;

namespace Shiny.Notifications.Tests;


public class ChannelExtensionsTests
{
    // -------- Channel.AssertValid --------

    [Fact]
    public void Channel_MissingIdentifier_Throws()
    {
        var ch = new Channel { Identifier = "" };
        Assert.Throws<ArgumentException>(() => ch.AssertValid());
    }


    [Fact]
    public void Channel_CustomSound_WithoutPath_Throws()
    {
        var ch = new Channel { Identifier = "x", Sound = ChannelSound.Custom };
        Assert.Throws<ArgumentException>(() => ch.AssertValid());
    }


    [Fact]
    public void Channel_NonCustomSound_WithPath_Throws()
    {
        var ch = new Channel { Identifier = "x", Sound = ChannelSound.Default, CustomSoundPath = "raw://ding" };
        Assert.Throws<ArgumentException>(() => ch.AssertValid());
    }


    [Fact]
    public void Channel_MinimalValid_Passes()
    {
        var ch = new Channel { Identifier = "x" };
        ch.AssertValid();
    }


    [Fact]
    public void Channel_CustomSoundWithPath_Passes()
    {
        var ch = new Channel { Identifier = "x", Sound = ChannelSound.Custom, CustomSoundPath = "raw://ding" };
        ch.AssertValid();
    }


    [Fact]
    public void Channel_CascadesActionValidation()
    {
        var bad = new ChannelAction { Identifier = "", Title = "OK" };
        var ch = new Channel { Identifier = "x", Actions = new List<ChannelAction> { bad } };
        Assert.Throws<ArgumentException>(() => ch.AssertValid());
    }


    // -------- ChannelAction.AssertValid --------

    [Fact]
    public void Action_MissingIdentifier_Throws()
    {
        var a = new ChannelAction { Identifier = "", Title = "OK" };
        Assert.Throws<ArgumentException>(() => a.AssertValid());
    }


    [Fact]
    public void Action_MissingTitle_Throws()
    {
        var a = new ChannelAction { Identifier = "yes", Title = "" };
        Assert.Throws<ArgumentException>(() => a.AssertValid());
    }


    [Fact]
    public void Action_BothFieldsPresent_Passes()
    {
        var a = new ChannelAction { Identifier = "yes", Title = "Yes" };
        a.AssertValid();
    }


    // -------- Channel.Create factory --------

    [Fact]
    public void Create_AssignsIdentifier_AndActions()
    {
        var a1 = new ChannelAction { Identifier = "yes", Title = "Yes" };
        var a2 = new ChannelAction { Identifier = "no", Title = "No" };

        var ch = Channel.Create("alerts", a1, a2);

        Assert.Equal("alerts", ch.Identifier);
        Assert.Equal(new[] { a1, a2 }, ch.Actions);
    }


    [Fact]
    public void Create_NoActions_LeavesEmptyList()
    {
        var ch = Channel.Create("alerts");
        Assert.Empty(ch.Actions);
    }


    [Fact]
    public void DefaultChannel_HasKnownIdAndLowImportance()
    {
        Assert.Equal("Notifications", Channel.Default.Identifier);
        Assert.Equal(ChannelImportance.Low, Channel.Default.Importance);
    }
}
