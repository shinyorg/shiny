using Shiny.Net.Discovery;
using Shiny.Net.Discovery.Internals;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class ServiceTypeParserTests
{
    [Theory]
    [InlineData("_http._tcp")]
    [InlineData("_http._tcp.")]
    [InlineData("_http._tcp.local")]
    [InlineData("_http._tcp.local.")]
    [InlineData("_HTTP._TCP")]
    public void Parse_AcceptsEveryCommonSpelling(string input)
    {
        var result = ServiceTypeParser.Parse(input);

        Assert.Equal("_http", result.Application);
        Assert.Equal("_tcp", result.Protocol);
        Assert.Equal("local", result.Domain);
        Assert.Equal("_http._tcp", result.ServiceType);
        Assert.Equal("_http._tcp.local", result.QualifiedName);
    }


    [Fact]
    public void Parse_DomainInTypeWinsOverTheArgument()
    {
        var result = ServiceTypeParser.Parse("_http._tcp.example.com", "local");
        Assert.Equal("example.com", result.Domain);
    }


    [Fact]
    public void Parse_KeepsUdpTransport()
        => Assert.Equal("_udp", ServiceTypeParser.Parse("_ntp._udp").Protocol);


    [Theory]
    [InlineData("", "empty")]
    [InlineData("_http", "application label and a transport label")]
    [InlineData("http._tcp", "underscore")]
    [InlineData("_http._sctp", "_tcp' or '_udp")]
    [InlineData("_waytoolongforalabel._tcp", "character limit")]
    [InlineData("_-http._tcp", "hyphen")]
    [InlineData("_http-._tcp", "hyphen")]
    [InlineData("_123._tcp", "at least one letter")]
    [InlineData("_ht tp._tcp", "letters, digits and hyphens")]
    [InlineData("_printer._sub._http._tcp", "subtypes")]
    public void Parse_RejectsBadInput(string input, string expectedInMessage)
    {
        var ex = Assert.Throws<MdnsException>(() => ServiceTypeParser.Parse(input));
        Assert.Contains(expectedInMessage, ex.Message);
    }


    [Fact]
    public void ValidateInstanceName_RejectsEmpty()
        => Assert.Throws<MdnsException>(() => ServiceTypeParser.ValidateInstanceName("  "));


    [Fact]
    public void ValidateInstanceName_RejectsOver63Bytes()
    {
        // 32 two-byte characters is 64 bytes, one past the DNS label limit
        var name = new string('é', 32);
        var ex = Assert.Throws<MdnsException>(() => ServiceTypeParser.ValidateInstanceName(name));
        Assert.Contains("64 bytes", ex.Message);
    }


    [Fact]
    public void ValidateInstanceName_AllowsSpacesAndUnicode()
        => ServiceTypeParser.ValidateInstanceName("Allan's Printer ⚡");


    [Fact]
    public void TrySplitFullName_SplitsInstanceFromType()
    {
        Assert.True(ServiceTypeParser.TrySplitFullName("Living Room._http._tcp.local.", out var instance, out var type));

        Assert.Equal("Living Room", instance);
        Assert.Equal("_http._tcp", type.ServiceType);
        Assert.Equal("local", type.Domain);
    }


    [Fact]
    public void TrySplitFullName_RejectsBareType()
        => Assert.False(ServiceTypeParser.TrySplitFullName("_http._tcp.local", out _, out _));
}
