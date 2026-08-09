using System.Xml;
using Shiny.Net.Discovery.Managed.WsDiscovery;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class WsdScopeMatcherTests
{
    static readonly XmlQualifiedName nvt = WsDiscoveryConstants.OnvifNetworkVideoTransmitter;
    static readonly XmlQualifiedName device = WsDiscoveryConstants.OnvifDevice;

    const string Rfc3986 = "http://docs.oasis-open.org/ws-dd/ns/discovery/2009/01/rfc3986";
    const string Strcmp0 = "http://schemas.xmlsoap.org/ws/2005/04/discovery/strcmp0";
    const string Uuid = "http://schemas.xmlsoap.org/ws/2005/04/discovery/uuid";
    const string Ldap = "http://schemas.xmlsoap.org/ws/2005/04/discovery/ldap";


    static bool MatchScopes(string[] probe, string[] target, string? matchBy = null)
        => WsdScopeMatcher.MatchesScopes(probe, target, matchBy, WsdVersionProfile.Ws2009);


    [Fact]
    public void AnEmptyProbeMatchesEverything()
    {
        Assert.True(WsdScopeMatcher.MatchesTypes(Array.Empty<XmlQualifiedName>(), new[] { nvt }));
        Assert.True(MatchScopes(Array.Empty<string>(), new[] { "onvif://www.onvif.org/type" }));
    }


    [Fact]
    public void EveryProbedTypeMustBePresent()
    {
        Assert.True(WsdScopeMatcher.MatchesTypes(new[] { nvt }, new[] { nvt, device }));
        Assert.True(WsdScopeMatcher.MatchesTypes(new[] { nvt, device }, new[] { device, nvt }));
        Assert.False(WsdScopeMatcher.MatchesTypes(new[] { nvt, device }, new[] { nvt }));
    }


    [Fact]
    public void TypesCompareOnNamespaceNotJustLocalName()
    {
        var sameNameOtherNamespace = new XmlQualifiedName("NetworkVideoTransmitter", "http://example.org/other");

        Assert.False(WsdScopeMatcher.MatchesTypes(new[] { nvt }, new[] { sameNameOtherNamespace }));
    }


    [Fact]
    public void DefaultRuleMatchesOnWholePathSegments()
    {
        Assert.True(MatchScopes(
            new[] { "onvif://www.onvif.org/type" },
            new[] { "onvif://www.onvif.org/type/video_encoder" }
        ));

        // a partial segment must not match, which is what makes this segment-wise rather than
        // a string prefix comparison
        Assert.False(MatchScopes(
            new[] { "onvif://www.onvif.org/ty" },
            new[] { "onvif://www.onvif.org/type/video_encoder" }
        ));
    }


    [Fact]
    public void DefaultRuleIsCaseInsensitiveOnAuthorityAndSensitiveOnPath()
    {
        Assert.True(MatchScopes(
            new[] { "ONVIF://WWW.ONVIF.ORG/type" },
            new[] { "onvif://www.onvif.org/type" }
        ));

        Assert.False(MatchScopes(
            new[] { "onvif://www.onvif.org/TYPE" },
            new[] { "onvif://www.onvif.org/type" }
        ));
    }


    [Fact]
    public void DefaultRuleIgnoresQueryAndFragment()
        => Assert.True(MatchScopes(
            new[] { "onvif://www.onvif.org/type?x=1#frag" },
            new[] { "onvif://www.onvif.org/type/video_encoder" }
        ));


    [Fact]
    public void ADeeperProbeScopeDoesNotMatchAShallowerTarget()
        => Assert.False(MatchScopes(
            new[] { "onvif://www.onvif.org/type/video_encoder" },
            new[] { "onvif://www.onvif.org/type" }
        ));


    [Fact]
    public void DifferentSchemeOrAuthorityNeverMatches()
    {
        Assert.False(MatchScopes(new[] { "onvif://www.onvif.org/type" }, new[] { "http://www.onvif.org/type" }));
        Assert.False(MatchScopes(new[] { "onvif://www.onvif.org/type" }, new[] { "onvif://other.example/type" }));
    }


    [Fact]
    public void Strcmp0IsExact()
    {
        Assert.True(MatchScopes(new[] { "onvif://a/b" }, new[] { "onvif://a/b" }, Strcmp0));
        Assert.False(MatchScopes(new[] { "onvif://a/b" }, new[] { "onvif://a/b/c" }, Strcmp0));
        Assert.False(MatchScopes(new[] { "onvif://A/b" }, new[] { "onvif://a/b" }, Strcmp0));
    }


    [Fact]
    public void UuidRuleIgnoresPrefixAndCasing()
    {
        Assert.True(MatchScopes(
            new[] { "urn:uuid:2F402F80-DA50-11E1-9B23-0017880924F0" },
            new[] { "urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0" },
            Uuid
        ));

        Assert.False(MatchScopes(
            new[] { "urn:uuid:2f402f80-da50-11e1-9b23-0017880924f0" },
            new[] { "urn:uuid:11111111-2222-3333-4444-555555555555" },
            Uuid
        ));
    }


    [Fact]
    public void LdapNeverMatchesBecauseItIsNotImplemented()
        => Assert.False(MatchScopes(new[] { "ldap:///ou=x" }, new[] { "ldap:///ou=x" }, Ldap));


    [Fact]
    public void AnUnknownMatchByRuleNeverMatches()
        => Assert.False(MatchScopes(
            new[] { "onvif://www.onvif.org/type" },
            new[] { "onvif://www.onvif.org/type" },
            "http://example.org/discovery/madeup"
        ));


    [Fact]
    public void EveryProbedScopeMustBeCovered()
    {
        var target = new[] { "onvif://www.onvif.org/type/video_encoder", "onvif://www.onvif.org/name/Door" };

        Assert.True(MatchScopes(new[] { "onvif://www.onvif.org/type", "onvif://www.onvif.org/name" }, target));
        Assert.False(MatchScopes(new[] { "onvif://www.onvif.org/type", "onvif://www.onvif.org/location" }, target));
    }


    [Fact]
    public void BothVersionsOfTheDefaultRuleBehaveIdentically()
    {
        var probe = new[] { "onvif://www.onvif.org/type" };
        var target = new[] { "onvif://www.onvif.org/type/video_encoder" };

        Assert.True(WsdScopeMatcher.MatchesScopes(probe, target, null, WsdVersionProfile.Ws2005));
        Assert.True(WsdScopeMatcher.MatchesScopes(probe, target, Rfc3986, WsdVersionProfile.Ws2009));
    }
}


public class WsdTargetTests
{
    [Theory]
    [InlineData("name", "Front Door")]
    [InlineData("hardware", "ACME-1")]
    [InlineData("location", "Lobby/West")]
    [InlineData("missing", null)]
    public void ScopeValuesAreExtractedAndUnescaped(string category, string? expected)
    {
        var target = new WsdTarget
        {
            EndpointReference = "urn:uuid:x",
            Scopes = new[]
            {
                "onvif://www.onvif.org/name/Front%20Door",
                "onvif://www.onvif.org/hardware/ACME-1",
                "onvif://www.onvif.org/location/Lobby%2FWest",
                "onvif://www.onvif.org/Profile/Streaming"
            }
        };

        Assert.Equal(expected, target.GetScopeValue(category));
    }


    [Fact]
    public void OnvifCamerasAreRecognisedByType()
    {
        var camera = new WsdTarget
        {
            EndpointReference = "urn:uuid:x",
            Types = new[] { WsDiscoveryConstants.OnvifNetworkVideoTransmitter }
        };
        var printer = new WsdTarget
        {
            EndpointReference = "urn:uuid:y",
            Types = new[] { WsDiscoveryConstants.WsdpDevice }
        };

        Assert.True(camera.IsOnvifCamera);
        Assert.False(printer.IsOnvifCamera);
    }
}
