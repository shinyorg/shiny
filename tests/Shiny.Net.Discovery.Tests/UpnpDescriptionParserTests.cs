using System.Text;
using Shiny.Net.Discovery.Managed.Ssdp;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


public class UpnpDescriptionParserTests
{
    static readonly Uri location = new("http://192.168.1.5:8080/desc/device.xml");


    static UpnpDeviceDescription Parse(string xml, Uri? from = null)
        => UpnpDescriptionParser.Parse(Encoding.UTF8.GetBytes(xml), from ?? location);


    const string FullDocument = """
        <?xml version="1.0"?>
        <root xmlns="urn:schemas-upnp-org:device-1-0">
          <specVersion><major>1</major><minor>0</minor></specVersion>
          <device>
            <deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>
            <friendlyName>Living Room</friendlyName>
            <manufacturer>Acme</manufacturer>
            <manufacturerURL>http://acme.example/</manufacturerURL>
            <modelName>Speaker</modelName>
            <modelNumber>S1</modelNumber>
            <serialNumber>SN-9</serialNumber>
            <UDN>uuid:2f402f80-da50-11e1-9b23-0017880924f0</UDN>
            <UPC>012345678905</UPC>
            <presentationURL>/index.html</presentationURL>
            <iconList>
              <icon>
                <mimetype>image/png</mimetype>
                <width>48</width><height>48</height><depth>24</depth>
                <url>/icon48.png</url>
              </icon>
            </iconList>
            <serviceList>
              <service>
                <serviceType>urn:schemas-upnp-org:service:ContentDirectory:1</serviceType>
                <serviceId>urn:upnp-org:serviceId:ContentDirectory</serviceId>
                <SCPDURL>/scpd/cd.xml</SCPDURL>
                <controlURL>/control/cd</controlURL>
                <eventSubURL></eventSubURL>
              </service>
            </serviceList>
            <deviceList>
              <device>
                <deviceType>urn:schemas-upnp-org:device:Embedded:1</deviceType>
                <friendlyName>Embedded</friendlyName>
                <UDN>uuid:11111111-2222-3333-4444-555555555555</UDN>
              </device>
            </deviceList>
          </device>
        </root>
        """;


    [Fact]
    public void FullDocumentIsParsed()
    {
        var device = Parse(FullDocument);

        Assert.Equal("uuid:2f402f80-da50-11e1-9b23-0017880924f0", device.Udn);
        Assert.Equal("Living Room", device.FriendlyName);
        Assert.Equal("Acme", device.Manufacturer);
        Assert.Equal("Speaker", device.ModelName);
        Assert.Equal("SN-9", device.SerialNumber);
        Assert.Equal("012345678905", device.Upc);
        Assert.Equal("urn:schemas-upnp-org:device:MediaServer:1", device.DeviceType);
    }


    [Fact]
    public void RelativeUrlsResolveAgainstTheFetchLocation()
    {
        var device = Parse(FullDocument);

        Assert.Equal("http://192.168.1.5:8080/index.html", device.PresentationUrl?.AbsoluteUri);
        Assert.Equal("http://192.168.1.5:8080/icon48.png", device.Icons[0].Url?.AbsoluteUri);
        Assert.Equal("http://192.168.1.5:8080/control/cd", device.Services[0].ControlUrl?.AbsoluteUri);
    }


    [Fact]
    public void AbsoluteUrlsArePreserved()
        => Assert.Equal("http://acme.example/", Parse(FullDocument).ManufacturerUrl?.AbsoluteUri);


    [Fact]
    public void IconsAndServicesAreParsed()
    {
        var device = Parse(FullDocument);

        var icon = Assert.Single(device.Icons);
        Assert.Equal("image/png", icon.MimeType);
        Assert.Equal(48, icon.Width);
        Assert.Equal(24, icon.Depth);

        var service = Assert.Single(device.Services);
        Assert.Equal("urn:schemas-upnp-org:service:ContentDirectory:1", service.ServiceType);
        Assert.Equal("urn:upnp-org:serviceId:ContentDirectory", service.ServiceId);
        // an empty eventSubURL means the service has no eventing
        Assert.Null(service.EventSubscriptionUrl);
    }


    [Fact]
    public void EmbeddedDevicesAreParsedAndFlattened()
    {
        var device = Parse(FullDocument);

        var embedded = Assert.Single(device.Devices);
        Assert.Equal("uuid:11111111-2222-3333-4444-555555555555", embedded.Udn);
        Assert.Equal(2, device.Flatten().Count());
    }


    [Fact]
    public void MissingNamespaceIsAccepted()
    {
        // plenty of shipped devices declare no default namespace at all
        var device = Parse("<root><device><friendlyName>Bare</friendlyName><UDN>uuid:x</UDN></device></root>");

        Assert.Equal("Bare", device.FriendlyName);
    }


    [Fact]
    public void WrongNamespaceIsAccepted()
    {
        var device = Parse("<root xmlns=\"urn:something-else\"><device><friendlyName>Odd</friendlyName><UDN>uuid:x</UDN></device></root>");

        Assert.Equal("Odd", device.FriendlyName);
    }


    [Fact]
    public void ByteOrderMarkIsHandled()
    {
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("<root><device><UDN>uuid:x</UDN><friendlyName>BOM</friendlyName></device></root>"))
            .ToArray();

        Assert.Equal("BOM", UpnpDescriptionParser.Parse(bytes, location).FriendlyName);
    }


    [Fact]
    public void UnknownDeclaredEncodingFallsBackToStrippingTheDeclaration()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"utf8\"?><root><device><UDN>uuid:x</UDN><friendlyName>Odd encoding</friendlyName></device></root>";

        Assert.Equal("Odd encoding", Parse(xml).FriendlyName);
    }


    [Fact]
    public void UrlBaseTakesPrecedenceOverTheLocation()
    {
        var xml = "<root><URLBase>http://192.168.1.5:8080/base/</URLBase><device><UDN>uuid:x</UDN><presentationURL>page.html</presentationURL></device></root>";

        Assert.Equal("http://192.168.1.5:8080/base/page.html", Parse(xml).PresentationUrl?.AbsoluteUri);
    }


    [Fact]
    public void UrlBaseWithoutATrailingSlashDoesNotLoseItsLastSegment()
    {
        // the classic UPnP base URL bug - Uri resolution would otherwise drop "/base"
        var xml = "<root><URLBase>http://192.168.1.5:8080/base</URLBase><device><UDN>uuid:x</UDN><presentationURL>page.html</presentationURL></device></root>";

        Assert.Equal("http://192.168.1.5:8080/base/page.html", Parse(xml).PresentationUrl?.AbsoluteUri);
    }


    [Fact]
    public void AUrlBaseOnADifferentHostIsIgnoredInFavourOfTheLocation()
    {
        // a stale URLBase after a DHCP change points somewhere unreachable
        var xml = "<root><URLBase>http://10.0.0.9/</URLBase><device><UDN>uuid:x</UDN><presentationURL>page.html</presentationURL></device></root>";

        Assert.Equal("http://192.168.1.5:8080/desc/page.html", Parse(xml).PresentationUrl?.AbsoluteUri);
    }


    [Fact]
    public void UdnIsNormalized()
        => Assert.Equal("uuid:abc", Parse("<root><device><UDN>{abc}</UDN></device></root>").Udn);


    [Fact]
    public void DeepNestingIsCapped()
    {
        var xml = new StringBuilder("<root><device><UDN>uuid:0</UDN>");
        const int depth = 20;

        for (var i = 1; i <= depth; i++)
            xml.Append($"<deviceList><device><UDN>uuid:{i}</UDN>");

        for (var i = 0; i < depth; i++)
            xml.Append("</device></deviceList>");

        xml.Append("</device></root>");

        // 1 root + 8 levels of embedded devices, not 21
        Assert.Equal(9, Parse(xml.ToString()).Flatten().Count());
    }


    [Theory]
    [InlineData("", "was empty")]
    [InlineData("not xml at all", "not valid XML")]
    [InlineData("<root></root>", "no <device> element")]
    public void BadDocumentsThrowWithAnExplanation(string xml, string expectedMessage)
    {
        var ex = Assert.Throws<SsdpException>(() => Parse(xml));
        Assert.Contains(expectedMessage, ex.Message);
    }


    [Fact]
    public void DoctypeIsRefused()
    {
        // entity expansion from a document handed to us by a stranger on the network
        var xml = "<!DOCTYPE root [<!ENTITY x \"boom\">]><root><device><UDN>uuid:x</UDN></device></root>";

        Assert.Throws<SsdpException>(() => Parse(xml));
    }
}
