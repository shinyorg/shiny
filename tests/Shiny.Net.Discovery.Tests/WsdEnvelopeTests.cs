using System.Text;
using System.Xml;
using Shiny.Net.Discovery.Managed.WsDiscovery;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class WsdEnvelopeTests
{
    static readonly XmlQualifiedName onvifNvt = WsDiscoveryConstants.OnvifNetworkVideoTransmitter;


    static WsdMessage Parse(string xml)
    {
        var message = WsdEnvelopeReader.TryParse(Encoding.UTF8.GetBytes(xml));
        Assert.NotNull(message);
        return message!;
    }


    static WsdMessage ParseBytes(byte[] data)
    {
        var message = WsdEnvelopeReader.TryParse(data);
        Assert.NotNull(message);
        return message!;
    }


    [Theory]
    [InlineData(nameof(WsdVersionProfile.Ws2005))]
    [InlineData(nameof(WsdVersionProfile.Ws2009))]
    public void ProbeRoundTripsInBothProfiles(string profileName)
    {
        var profile = profileName == nameof(WsdVersionProfile.Ws2005) ? WsdVersionProfile.Ws2005 : WsdVersionProfile.Ws2009;
        var bytes = WsdEnvelopeWriter.Probe(profile, "urn:uuid:1234", new[] { onvifNvt }, Array.Empty<string>(), null);

        var message = ParseBytes(bytes);

        Assert.Equal(WsdMessageNames.Probe, message.MessageName);
        Assert.Equal(profile.Profile, message.Profile.Profile);
        Assert.Equal("urn:uuid:1234", message.MessageId);
        Assert.Equal(onvifNvt, Assert.Single(message.Probe!.Types));
    }


    [Fact]
    public void ProbeMatchesRoundTripsWithRelatesToAndAppSequence()
    {
        var match = new WsdMatch(
            "urn:uuid:aaaa",
            new[] { onvifNvt },
            new[] { "onvif://www.onvif.org/type/video_encoder" },
            new[] { "http://192.168.1.9/onvif/device_service" },
            5
        );

        var bytes = WsdEnvelopeWriter.Matches(
            WsdVersionProfile.Ws2005,
            WsdMessageNames.ProbeMatches,
            "urn:uuid:reply",
            "urn:uuid:request",
            new WsdAppSequence(99, null, 3),
            new[] { match }
        );

        var message = ParseBytes(bytes);

        Assert.Equal(WsdMessageNames.ProbeMatches, message.MessageName);
        Assert.Equal("urn:uuid:request", message.RelatesTo);
        Assert.Equal(99u, message.AppSequence!.Value.InstanceId);
        Assert.Equal(3u, message.AppSequence!.Value.MessageNumber);

        var parsed = Assert.Single(message.Matches);
        Assert.Equal("urn:uuid:aaaa", parsed.EndpointReference);
        Assert.Equal(onvifNvt, Assert.Single(parsed.Types));
        Assert.Equal("onvif://www.onvif.org/type/video_encoder", Assert.Single(parsed.Scopes));
        Assert.Equal("http://192.168.1.9/onvif/device_service", Assert.Single(parsed.XAddrs));
        Assert.Equal(5u, parsed.MetadataVersion);
    }


    [Fact]
    public void HelloAndByeCarryTheTargetInline()
    {
        var match = new WsdMatch("urn:uuid:bbbb", Array.Empty<XmlQualifiedName>(), Array.Empty<string>(), Array.Empty<string>(), 1);

        var hello = ParseBytes(WsdEnvelopeWriter.Hello(WsdVersionProfile.Ws2005, "urn:uuid:h", new WsdAppSequence(1, null, 1), match));
        var bye = ParseBytes(WsdEnvelopeWriter.Bye(WsdVersionProfile.Ws2005, "urn:uuid:b", new WsdAppSequence(1, null, 2), match));

        Assert.Equal(WsdMessageNames.Hello, hello.MessageName);
        Assert.Equal("urn:uuid:bbbb", Assert.Single(hello.Matches).EndpointReference);

        Assert.Equal(WsdMessageNames.Bye, bye.MessageName);
        Assert.Equal("urn:uuid:bbbb", Assert.Single(bye.Matches).EndpointReference);
    }


    [Fact]
    public void ResolveCarriesItsTarget()
    {
        var message = ParseBytes(WsdEnvelopeWriter.Resolve(WsdVersionProfile.Ws2009, "urn:uuid:r", "urn:uuid:target"));

        Assert.Equal(WsdMessageNames.Resolve, message.MessageName);
        Assert.Equal("urn:uuid:target", message.ResolveTarget);
    }


    [Fact]
    public void QNamePrefixDeclaredOnTheEnvelopeIsResolved()
    {
        var message = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"
                        xmlns:a="http://schemas.xmlsoap.org/ws/2004/08/addressing"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery"
                        xmlns:dn="http://www.onvif.org/ver10/network/wsdl">
              <s:Header>
                <a:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>
                <a:MessageID>urn:uuid:1</a:MessageID>
              </s:Header>
              <s:Body><d:Probe><d:Types>dn:NetworkVideoTransmitter</d:Types></d:Probe></s:Body>
            </s:Envelope>
            """);

        Assert.Equal(onvifNvt, Assert.Single(message.Probe!.Types));
    }


    [Fact]
    public void QNamePrefixDeclaredOnTheTypesElementIsResolved()
    {
        // legal, and the case that breaks naive parsers that only look at the envelope
        var message = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"
                        xmlns:a="http://schemas.xmlsoap.org/ws/2004/08/addressing"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery">
              <s:Header>
                <a:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>
              </s:Header>
              <s:Body>
                <d:Probe>
                  <d:Types xmlns:dn="http://www.onvif.org/ver10/network/wsdl">dn:NetworkVideoTransmitter</d:Types>
                </d:Probe>
              </s:Body>
            </s:Envelope>
            """);

        Assert.Equal(onvifNvt, Assert.Single(message.Probe!.Types));
    }


    [Fact]
    public void MultipleQNamesWithDifferentNamespacesRoundTrip()
    {
        var types = new[] { onvifNvt, WsDiscoveryConstants.OnvifDevice, WsDiscoveryConstants.WsdpDevice };
        var bytes = WsdEnvelopeWriter.Probe(WsdVersionProfile.Ws2005, "urn:uuid:1", types, Array.Empty<string>(), null);

        Assert.Equal(types, ParseBytes(bytes).Probe!.Types);
    }


    [Fact]
    public void AnUnprefixedQNameResolvesAgainstTheDefaultNamespace()
    {
        var message = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery">
              <s:Body>
                <d:Probe><d:Types xmlns="http://example.org/types">Thing</d:Types></d:Probe>
              </s:Body>
            </s:Envelope>
            """);

        Assert.Equal(new XmlQualifiedName("Thing", "http://example.org/types"), Assert.Single(message.Probe!.Types));
    }


    [Fact]
    public void ScopesMatchByAttributeIsRead()
    {
        var message = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery">
              <s:Body>
                <d:Probe>
                  <d:Scopes MatchBy="http://schemas.xmlsoap.org/ws/2005/04/discovery/strcmp0">onvif://www.onvif.org/type</d:Scopes>
                </d:Probe>
              </s:Body>
            </s:Envelope>
            """);

        Assert.Equal("http://schemas.xmlsoap.org/ws/2005/04/discovery/strcmp0", message.Probe!.MatchBy);
        Assert.Equal("onvif://www.onvif.org/type", Assert.Single(message.Probe!.Scopes));
    }


    [Fact]
    public void AMessageWithoutAppSequenceIsStillAccepted()
    {
        // cheap cameras omit it entirely
        var message = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"
                        xmlns:a="http://schemas.xmlsoap.org/ws/2004/08/addressing"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery">
              <s:Header><a:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/Hello</a:Action></s:Header>
              <s:Body><d:Hello><a:EndpointReference><a:Address>urn:uuid:cccc</a:Address></a:EndpointReference></d:Hello></s:Body>
            </s:Envelope>
            """);

        Assert.Null(message.AppSequence);
        Assert.Equal("urn:uuid:cccc", Assert.Single(message.Matches).EndpointReference);
    }


    [Fact]
    public void MessageNameFallsBackToTheBodyElementWhenActionIsMissing()
    {
        var message = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"
                        xmlns:a="http://schemas.xmlsoap.org/ws/2004/08/addressing"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery">
              <s:Body><d:Bye><a:EndpointReference><a:Address>urn:uuid:dddd</a:Address></a:EndpointReference></d:Bye></s:Body>
            </s:Envelope>
            """);

        Assert.Equal(WsdMessageNames.Bye, message.MessageName);
    }


    [Theory]
    [InlineData("urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0", "urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    [InlineData("2f402f80-da50-11e1-9b23-0017880924f0", "urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    [InlineData("urn:uuid:{2f402f80-da50-11e1-9b23-0017880924f0}", "urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    [InlineData("URN:UUID:2F402F80-DA50-11E1-9B23-0017880924F0", "urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0")]
    // an EPR does not have to be a UUID, and a URL must survive untouched
    [InlineData("http://192.168.1.9/device", "http://192.168.1.9/device")]
    public void EndpointReferencesAreNormalized(string raw, string expected)
        => Assert.Equal(expected, WsdEnvelopeReader.NormalizeEndpointReference(raw));


    [Theory]
    [InlineData("")]
    [InlineData("not xml")]
    [InlineData("<html><body>hello</body></html>")]
    public void GarbageIsRejectedRatherThanThrown(string xml)
        => Assert.Null(WsdEnvelopeReader.TryParse(Encoding.UTF8.GetBytes(xml)));


    [Fact]
    public void AnUnknownDiscoveryNamespaceIsRejected()
    {
        var xml = """
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope" xmlns:d="http://example.org/other">
              <s:Body><d:Probe /></s:Body>
            </s:Envelope>
            """;

        Assert.Null(WsdEnvelopeReader.TryParse(Encoding.UTF8.GetBytes(xml)));
    }


    [Fact]
    public void OversizedDatagramsAreRejected()
        => Assert.Null(WsdEnvelopeReader.TryParse(new byte[WsdEnvelopeReader.MaxMessageSize + 1]));


    [Fact]
    public void ProfilesResolveByNamespaceAndSelectionExpands()
    {
        Assert.Equal(WsDiscoveryProfile.Ws2005, WsdVersionProfile.ForNamespace(WsdVersionProfile.Ws2005.DiscoveryNamespace)!.Profile);
        Assert.Equal(WsDiscoveryProfile.Ws2009, WsdVersionProfile.ForNamespace(WsdVersionProfile.Ws2009.DiscoveryNamespace)!.Profile);
        Assert.Null(WsdVersionProfile.ForNamespace("http://example.org"));

        Assert.Equal(2, WsdVersionProfile.Selected(WsDiscoveryProfile.All).Count());
        Assert.Single(WsdVersionProfile.Selected(WsDiscoveryProfile.Ws2005));
        Assert.Empty(WsdVersionProfile.Selected(WsDiscoveryProfile.None));
    }
}
