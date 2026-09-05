---
name: shiny-discovery
description: Generate code using Shiny.Net.Discovery for cross-platform local network discovery - mDNS/DNS-SD (Bonjour/Zeroconf), SSDP/UPnP, and WS-Discovery (ONVIF) browsing, resolution, and publishing on iOS, Android, Mac Catalyst, macOS, Windows, and Linux
auto_invoke: true
triggers:
  - mDNS
  - DNS-SD
  - Bonjour
  - Zeroconf
  - SSDP
  - UPnP
  - DLNA
  - M-SEARCH
  - ssdp:discover
  - ssdp:all
  - upnp:rootdevice
  - WS-Discovery
  - WSD
  - WSDAPI
  - ONVIF
  - ONVIF camera
  - IP camera discovery
  - find a printer on the network
  - find a media server
  - find a Chromecast
  - find a Sonos
  - service discovery
  - network discovery
  - local network discovery
  - discover devices on the network
  - find devices on LAN
  - advertise a service
  - publish a service
  - IMdnsManager
  - ISsdpManager
  - IWsDiscoveryManager
  - SsdpDevice
  - SsdpBrowseConfig
  - SsdpDeviceRegistration
  - UpnpDeviceDescription
  - WsdTarget
  - WsdProbeConfig
  - WsDiscoveryRegistration
  - AddSsdp
  - AddWsDiscovery
  - DiscoveryPermissionException
  - multicast lock
  - MulticastLock
  - ACCESS_LOCAL_NETWORK
  - CHANGE_WIFI_MULTICAST_STATE
  - MdnsService
  - MdnsServiceRegistration
  - MdnsBrowseConfig
  - MdnsBrowseResult
  - MdnsBrowseStatus
  - IMdnsPublication
  - MdnsConstants
  - AddMdns
  - Shiny.Net.Discovery
  - BrowseOnce
  - _http._tcp
  - NSBonjourServices
  - NSLocalNetworkUsageDescription
  - NsdManager
  - NSNetService
  - TXT records
  - SRV record
  - multicast entitlement
---

# Shiny.Net.Discovery Skill

You are an expert in Shiny.Net.Discovery, a cross-platform local network discovery library covering
three protocols: **mDNS/DNS-SD** (Bonjour/Zeroconf), **SSDP/UPnP**, and **WS-Discovery**.

## When to Use This Skill

Invoke this skill when the user wants to:
- Discover services or devices on the local network (printers, Chromecasts, IoT devices, peer apps)
- Advertise their own app as a discoverable service
- Find UPnP/DLNA devices, routers, Sonos, Roku, or smart TVs (SSDP)
- Find ONVIF cameras, WSD printers/scanners, or Windows machines (WS-Discovery)
- Read TXT record metadata, or a UPnP device description
- Resolve a known service instance to a host, port, and IP addresses
- Ask about Bonjour/mDNS on iOS without the multicast entitlement
- Register any of the three protocols in DI

## Choosing a protocol

| Looking for | Use | Registration |
|-------------|-----|--------------|
| Apple devices, AirPlay, IPP printers, peer apps, your own services | mDNS/DNS-SD | `AddMdns()` |
| Routers, UPnP/DLNA media servers, Sonos, Roku, smart TVs | SSDP | `AddSsdp()` |
| ONVIF IP cameras, WSD printers/scanners, Windows machines | WS-Discovery | `AddWsDiscovery()` |

They are independent - register only what you need. A device that speaks one usually does not
speak the others.

## Library Overview

| Item       | Value                                                                                     |
|------------|-------------------------------------------------------------------------------------------|
| GitHub     | https://github.com/shinyorg/shiny                                                          |
| NuGet      | `Shiny.Net.Discovery`                                                                      |
| Namespace  | `Shiny.Net.Discovery` (types); `Shiny` (registration extensions)                            |
| Platforms  | iOS, tvOS, Mac Catalyst, macOS, Android, Windows, Linux, server .NET                        |

### How each platform is backed

**mDNS** delegates to the OS wherever there is an API:

| Platform                     | Implementation                | Why                                                       |
|------------------------------|-------------------------------|-----------------------------------------------------------|
| iOS / tvOS / Mac Catalyst / macOS | `NSNetService` (Bonjour) | Goes through the system mDNSResponder, so **no** `com.apple.developer.networking.multicast` entitlement |
| Android                      | `NsdManager`                  | No `CHANGE_WIFI_MULTICAST_STATE` and no `WifiManager.MulticastLock` |
| Windows / Linux / server .NET| Managed responder on UDP 5353 | No OS DNS-SD API to lean on; dependency-free, AOT-safe     |

**SSDP and WS-Discovery are managed on every platform**, including iOS and Android. This is not a
design choice - neither `NsdManager` nor Apple's Bonjour stack can speak these protocols, and no
OS exposes any other API for them. They therefore use raw UDP multicast everywhere.

### ⚠️ The entitlement rule differs by protocol

**For mDNS: do not tell users they need the iOS multicast entitlement.** That requirement applies
to raw multicast sockets, which the mDNS implementation deliberately avoids on Apple platforms.

**For SSDP and WS-Discovery the opposite is true** - they *do* need it on iOS, and it is
approval-gated by Apple:

| Platform | mDNS | SSDP / WS-Discovery |
|----------|------|---------------------|
| iOS      | nothing | **`com.apple.developer.networking.multicast`** - request at https://developer.apple.com/contact/request/networking-multicast, granted per developer team. Cannot be tested in the simulator |
| Mac Catalyst / macOS | sandbox network entitlements | same sandbox entitlements; **not** the multicast entitlement (iOS only). macOS 15+ also prompts for Local Network |
| Android  | nothing | `CHANGE_WIFI_MULTICAST_STATE` (the lock is acquired for you) **plus** `ACCESS_LOCAL_NETWORK` when targeting SDK 37+ |
| Windows  | firewall; MSIX capability | same |
| Linux    | firewall | same; Docker bridge networking does not work |

A missing permission throws `DiscoveryPermissionException` with a message naming exactly what to
add, rather than silently returning nothing.

## Setup

```csharp
using Shiny;

builder.Services.AddMdns();          // IMdnsManager
builder.Services.AddSsdp();          // ISsdpManager
builder.Services.AddWsDiscovery();   // IWsDiscoveryManager
```

Register only the protocols you use. `AddSsdp()` also registers a named `HttpClient`
(`SsdpConstants.HttpClientName`) for fetching device descriptions, with redirects and cookies
disabled deliberately - you can reconfigure it with `services.AddHttpClient(SsdpConstants.HttpClientName)`.

### iOS / Mac Catalyst / macOS — Info.plist (required)

Browsing silently returns **nothing** if the service type is not declared. Both keys are required:

```xml
<key>NSLocalNetworkUsageDescription</key>
<string>This app discovers nearby devices on your local network.</string>
<key>NSBonjourServices</key>
<array>
    <string>_myapp._tcp</string>
    <string>_http._tcp</string>
</array>
```

Every service type the app browses for **and** publishes must be listed in `NSBonjourServices`.
The local-network permission prompt appears automatically on first use; there is no API to query
or pre-request it.

### Mac Catalyst / macOS — App Sandbox entitlements (csproj)

For mDNS, iOS needs no entitlement at all. Mac Catalyst and macOS run sandboxed, so network access
must be declared — browsing/resolving needs the client entitlement, publishing also needs the
server one. These apply to all three protocols:

```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">
    <CustomEntitlements Include="com.apple.security.network.client" Type="Boolean" Value="true" />
    <!-- publish/advertise only -->
    <CustomEntitlements Include="com.apple.security.network.server" Type="Boolean" Value="true" />
</ItemGroup>
```

Still **not** `com.apple.developer.networking.multicast` — that is for raw multicast sockets.

### Android — AndroidManifest.xml

No runtime permission is needed, but declare:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### Windows / Linux

The managed responder binds UDP 5353. It sets `SO_REUSEADDR` (plus `SO_REUSEPORT` on Unix) so it
coexists with `avahi-daemon`, `mDNSResponder`, or the Windows DNS client. Ensure the firewall
allows UDP 5353.

A packaged Windows app (MSIX/WinUI) also needs the local-network capability in
`Package.appxmanifest`, otherwise the responder sends and receives nothing:

```xml
<Capabilities>
    <Capability Name="privateNetworkClientServer" />
</Capabilities>
```

### SSDP / WS-Discovery — extra platform setup

Everything above still applies. These two protocols additionally need the following, because they
use raw multicast rather than an OS discovery API.

**iOS — the multicast entitlement (blocking).** Apple grants this per developer team on request;
budget days to weeks. Without it, sends fail and nothing is received.

```xml
<!-- Entitlements.plist -->
<key>com.apple.developer.networking.multicast</key>
<true/>
```

Also set `NSLocalNetworkUsageDescription` in Info.plist. **This cannot be tested in the simulator** -
local network privacy is not enforced there, so the simulator neither reproduces the failure nor
proves the fix. `NSBonjourServices` is irrelevant to SSDP/WSD; it only gates mDNS.

**Android:**

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<!-- required, or sends succeed and nothing is ever received over Wi-Fi -->
<uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
<!-- runtime permission, mandatory when targeting SDK 37 (Android 17) or later -->
<uses-permission android:name="android.permission.ACCESS_LOCAL_NETWORK" />
```

The `WifiManager.MulticastLock` is acquired and released automatically for the lifetime of a
browse or publication - do **not** write code to manage it. `ACCESS_LOCAL_NETWORK` is a runtime
(dangerous) permission in the `NEARBY_DEVICES` group, so request it before discovering.

**Ports:** SSDP is UDP 1900, WS-Discovery is UDP 3702, both on group 239.255.255.250 (and
`ff02::c`). On Windows the system `SSDPSRV` and `FDResPub` services already hold those ports;
`SO_REUSEADDR` is set so this coexists with them.

**Docker:** bridge networking does not work - NAT does not rewrite the addresses embedded in
`LOCATION` and `XAddrs`. Use `--network host` or macvlan.

## mDNS API Reference

### IMdnsManager

```csharp
public interface IMdnsManager
{
    IAsyncEnumerable<MdnsBrowseResult> Browse(MdnsBrowseConfig config, CancellationToken ct = default);
    Task<MdnsService?> Resolve(string instanceName, string serviceType, TimeSpan? timeout = null, CancellationToken ct = default);
    Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default);
}
```

Extension helpers (`MdnsExtensions`):

```csharp
IAsyncEnumerable<MdnsBrowseResult> Browse(string serviceType, CancellationToken ct = default);
Task<IReadOnlyList<MdnsService>> BrowseOnce(string serviceType, TimeSpan? scanTime = null, CancellationToken ct = default);
Task<IMdnsPublication> Publish(string instanceName, string serviceType, int port, CancellationToken ct = default);
```

### Browsing (live)

`Browse` **never completes on its own** — it runs until the token is cancelled. Always pass a
`CancellationToken`.

```csharp
await foreach (var result in mdns.Browse("_http._tcp", ct))
{
    switch (result.Status)
    {
        case MdnsBrowseStatus.Found:
            // upsert keyed on result.Service.FullName - a re-announcing service emits Found again
            devices[result.Service.FullName] = result.Service;
            break;

        case MdnsBrowseStatus.Lost:
            // only InstanceName/ServiceType/Domain are populated on a Lost result
            devices.Remove(result.Service.FullName);
            break;
    }
}
```

### Browsing (one-shot scan)

```csharp
var found = await mdns.BrowseOnce("_ipp._tcp", TimeSpan.FromSeconds(5), ct);
foreach (var printer in found)
    Console.WriteLine($"{printer.InstanceName} => {printer.GetEndPoint()}");
```

### Publishing

You are responsible for having something listening on the port — publishing only advertises it.

```csharp
await using var publication = await mdns.Publish(
    new MdnsServiceRegistration("Allan's Laptop", "_myapp._tcp", 8080)
    {
        TxtRecords = new Dictionary<string, string>
        {
            ["version"] = "2",
            ["path"] = "/api"
        }
    },
    ct
);

// ALWAYS read the name back - the responder renames on conflict ("Allan's Laptop (2)")
Console.WriteLine($"advertising as {publication.InstanceName}");
```

Disposing the publication sends a goodbye packet and stops advertising.

### Resolving a known instance

```csharp
var service = await mdns.Resolve("Allan's Laptop", "_myapp._tcp", TimeSpan.FromSeconds(5), ct);
if (service?.IsResolved == true)
    await socket.ConnectAsync(service.GetEndPoint()!);
```

Returns `null` when the instance did not answer within the timeout.

## mDNS Models

### MdnsService

| Member          | Notes                                                                     |
|-----------------|---------------------------------------------------------------------------|
| `InstanceName`  | Human readable, unescaped; may contain spaces, dots, and UTF8              |
| `ServiceType`   | eg `_http._tcp`                                                           |
| `Domain`        | Almost always `local`                                                     |
| `HostName`      | SRV target, eg `printer.local`; null when unresolved                       |
| `Port`          | 0 when unresolved                                                          |
| `Addresses`     | `IReadOnlyList<IPAddress>`; often both IPv4 and IPv6                       |
| `TxtRecords`    | Case-insensitive key/value map; empty when there are none                   |
| `FullName`      | `"Allan's Laptop._myapp._tcp.local"` — use this as the dictionary key       |
| `IsResolved`    | `Port > 0 && Addresses.Count > 0`                                          |
| `GetEndPoint(family?)` | First matching `IPEndPoint`, or null when unresolved                |

TXT helpers:

```csharp
string? path = service.GetTxt("path");
int version  = service.GetTxt<int>("version", 1);   // any IParsable<T>, falls back on miss
bool secure  = service.GetTxt<bool>("secure");
```

### MdnsBrowseConfig

```csharp
new MdnsBrowseConfig("_http._tcp")
{
    Domain = "local",                             // mDNS only supports "local"
    ResolveServices = true,                       // false = bare instance names, fastest
    ResolveTimeout = TimeSpan.FromSeconds(5)      // per instance, then emitted unresolved
}
```

### MdnsServiceRegistration

```csharp
new MdnsServiceRegistration("Instance Name", "_myapp._tcp", 8080)
{
    Domain = "local",
    TxtRecords = new Dictionary<string, string> { ["k"] = "v" }
}
```

## Service Type Rules (RFC 6763 §7)

- Format is `_<application>._tcp` or `_<application>._udp`. `.local` / a trailing dot are optional
  on input and stripped.
- The application label is **1–15 characters**, letters/digits/hyphens only, must contain at least
  one letter, and may not start or end with a hyphen.
- Anything invalid throws `MdnsException` with a message explaining exactly what is wrong.
- **Service subtypes (`_sub`) are not supported** and are rejected.
- Instance names are a single DNS-SD label: spaces and UTF8 are fine, but they must encode to
  63 bytes or fewer.

## SSDP / UPnP API Reference

### ISsdpManager

```csharp
public interface ISsdpManager
{
    IAsyncEnumerable<SsdpBrowseResult> Browse(SsdpBrowseConfig config, CancellationToken ct = default);
    Task<UpnpDeviceDescription> GetDescription(SsdpDevice device, Func<Uri, IPAddress, bool>? urlFilter = null, CancellationToken ct = default);
    Task<ISsdpPublication> Publish(SsdpDeviceRegistration registration, CancellationToken ct = default);
}
```

Extension helpers (`SsdpExtensions`):

```csharp
IAsyncEnumerable<SsdpBrowseResult> Browse(string searchTarget, CancellationToken ct = default);
Task<IReadOnlyList<SsdpDevice>> Search(string searchTarget, TimeSpan? scanTime = null, CancellationToken ct = default);
Task<IReadOnlyList<SsdpDevice>> SearchAll(TimeSpan? scanTime = null, CancellationToken ct = default);
Task<ISsdpPublication> Publish(string udn, Uri location, CancellationToken ct = default);
```

### One-shot scan

```csharp
foreach (var device in await ssdp.SearchAll(TimeSpan.FromSeconds(5), ct))
    Console.WriteLine($"{device.Udn} {device.Server} => {device.Location}");
```

### Live browse

`Browse` **never completes on its own** — always pass a `CancellationToken`.

```csharp
await foreach (var result in ssdp.Browse(SsdpConstants.RootDevice, ct))
{
    switch (result.Status)
    {
        case SsdpBrowseStatus.Found:
            // upsert keyed on Udn - a device re-announcing emits Found again
            devices[result.Device.Udn] = result.Device;
            break;

        case SsdpBrowseStatus.Lost:
            // only Udn is populated on a Lost result
            devices.Remove(result.Device.Udn);
            break;
    }
}
```

### Fetching the device description

```csharp
var description = await ssdp.GetDescription(device, ct: ct);

Console.WriteLine(description.FriendlyName);        // "Living Room"
Console.WriteLine(description.ModelName);
foreach (var service in description.Services)
    Console.WriteLine(service.ServiceType);         // urn:schemas-upnp-org:service:...

// embedded devices share the parent's document
foreach (var d in description.Flatten())
    Console.WriteLine(d.Udn);
```

`GetDescription` throws `SsdpException` when the URL is refused by the fetch policy. **The default
policy requires the advertised `LOCATION` host to be the literal IP that sent the advertisement.**
This stops a device on the network from pointing the app at an arbitrary host. Override only when
a device legitimately advertises a different host:

```csharp
await ssdp.GetDescription(device, (url, source) => url.Host == "10.0.0.5", ct);
```

### Publishing

You must serve the description document yourself — this only advertises that it exists.

```csharp
await using var publication = await ssdp.Publish(
    new SsdpDeviceRegistration("uuid:" + Guid.NewGuid(), new Uri("http://192.168.1.20:8080/desc.xml"))
    {
        DeviceType = "urn:schemas-upnp-org:device:MediaServer:1",
        ServiceTypes = ["urn:schemas-upnp-org:service:ContentDirectory:1"],
        MaxAge = TimeSpan.FromMinutes(30)
    },
    ct
);
```

Advertisements are re-sent automatically before they expire, the boot id increments on network
change, and disposing sends `ssdp:byebye` for every advertised type.

### SSDP models

| Member | Notes |
|--------|-------|
| `SsdpDevice.Udn` | The device identity — **key your collections on this**, not the USN or location |
| `SsdpDevice.Location` | Description URL; null when only a goodbye was seen |
| `SsdpDevice.NotificationTypes` | Every advertisement seen: `upnp:rootdevice`, the UDN, device and service type URNs |
| `SsdpDevice.IsRootDevice` / `DeviceType` / `ServiceTypes` | Computed from `NotificationTypes` |
| `SsdpDevice.BootId` / `ConfigId` | UPnP 1.1 only; a `ConfigId` change means re-fetch the description |
| `SsdpDevice.Headers` | Every raw header — vendors put useful things here |
| `SsdpBrowseConfig.SearchTarget` | Defaults to `ssdp:all`. Use `SsdpConstants.RootDevice` to list devices without their services |
| `SsdpBrowseConfig.MaxWait` | The MX header, clamped to 1–5s. Below 2s misses slow embedded devices |

## WS-Discovery API Reference

### IWsDiscoveryManager

```csharp
public interface IWsDiscoveryManager
{
    IAsyncEnumerable<WsDiscoveryResult> Browse(WsdProbeConfig config, CancellationToken ct = default);
    Task<WsdTarget?> Resolve(string endpointReference, TimeSpan? timeout = null, CancellationToken ct = default);
    Task<IWsDiscoveryPublication> Publish(WsDiscoveryRegistration registration, CancellationToken ct = default);
}
```

Extension helpers (`WsDiscoveryExtensions`):

```csharp
IAsyncEnumerable<WsDiscoveryResult> Browse(CancellationToken ct = default);
IAsyncEnumerable<WsDiscoveryResult> Browse(XmlQualifiedName[] types, CancellationToken ct = default);
Task<IReadOnlyList<WsdTarget>> Probe(WsdProbeConfig? config = null, TimeSpan? scanTime = null, CancellationToken ct = default);
Task<IReadOnlyList<WsdTarget>> ProbeOnvifCameras(TimeSpan? scanTime = null, CancellationToken ct = default);
```

### Finding ONVIF cameras

```csharp
foreach (var camera in await wsd.ProbeOnvifCameras(TimeSpan.FromSeconds(5), ct))
{
    Console.WriteLine(camera.GetScopeValue("name"));      // "Front Door"
    Console.WriteLine(camera.GetScopeValue("hardware"));
    Console.WriteLine(camera.PreferredAddress);           // http://192.168.1.9/onvif/device_service
}
```

`ProbeOnvifCameras` deliberately sends an **untyped** probe and filters locally. A typed probe is
what the ONVIF spec describes but finds fewer cameras in practice — some answer only the older
`NetworkVideoTransmitter` type, some only the newer `Device` type, some only an untyped probe.

### Typed probe

```csharp
await foreach (var result in wsd.Browse([WsDiscoveryConstants.WsdpDevice], ct))
    Console.WriteLine(result.Target);
```

Built-in types: `WsDiscoveryConstants.OnvifNetworkVideoTransmitter`, `OnvifDevice`, `WsdpDevice`
(Windows machines and WSD printers/scanners). Types are `XmlQualifiedName` — namespace **and**
local name are compared, so two types with the same local name in different namespaces are distinct.

### Publishing

```csharp
await using var publication = await wsd.Publish(
    new WsDiscoveryRegistration("urn:uuid:" + Guid.NewGuid())
    {
        Types = [WsDiscoveryConstants.WsdpDevice],
        Scopes = ["onvif://www.onvif.org/name/My%20Service"],
        Addresses = [new Uri("http://192.168.1.20:8080/service")]
    },
    ct
);
```

Sends Hello on start, answers matching Probe and Resolve, and sends Bye on dispose.

### WS-Discovery models

| Member | Notes |
|--------|-------|
| `WsdTarget.EndpointReference` | The identity — **key your collections on this** |
| `WsdTarget.Types` | `IReadOnlyList<XmlQualifiedName>` |
| `WsdTarget.Scopes` | Raw scope URIs |
| `WsdTarget.Addresses` / `PreferredAddress` | Devices advertise stale and unreachable addresses; `PreferredAddress` picks the one matching the sender, else one on-link, else the first |
| `WsdTarget.GetScopeValue("name")` | Reads and URL-decodes an ONVIF-style scope segment |
| `WsdTarget.IsOnvifCamera` | True when the ONVIF NVT type is declared |
| `WsdTarget.Profile` | Which spec version the device answered in |
| `WsdProbeConfig.Profiles` | `WsDiscoveryProfile.All` by default — sends both 2005 and 2009 as separate datagrams |

**Both profiles are sent by default and you should leave it that way.** ONVIF cameras and Windows
speak 2005; many ignore 2009 entirely.

## Best Practices

- **Always pass a CancellationToken to `Browse`** — it is an infinite stream.
- **Key discovered services on `FullName`**, not `InstanceName` — `Found` is upsert semantics.
- **Read `IMdnsPublication.InstanceName` back** after publishing; conflicts cause renames.
- **Do not assume `IsResolved`** — an instance whose SRV/A never arrives is still emitted as
  `Found` once `ResolveTimeout` elapses, with `Port = 0` and no addresses.
- Set `ResolveServices = false` when you only need to list instance names; it avoids a resolve
  round trip per instance.
- Keep TXT records small — RFC 6763 recommends staying under 1300 bytes total so the record fits
  in one packet. A single `key=value` entry may not exceed 255 bytes (throws `MdnsException`).
- Wrap discovery in `try/catch (MdnsException)`; on Apple it is thrown when the service type is
  missing from `NSBonjourServices`.

### SSDP and WS-Discovery

- **Always pass a CancellationToken to `Browse`** — like mDNS, these are infinite streams. Use
  `Search`/`Probe` for a bounded scan.
- **Key on `SsdpDevice.Udn` and `WsdTarget.EndpointReference`.** Never key on the location, the
  USN, or the source address — a dual-homed or dual-stack device produces several of each for one
  physical device.
- **Catch `DiscoveryPermissionException` separately** and surface its message. It is the difference
  between a user fixing a manifest entry in a minute and concluding the network is empty. All the
  exception types derive from `DiscoveryException`, and `MdnsException` now does too.
- **Give scans 5 seconds.** ONVIF cameras and embedded UPnP devices are slow; 1–2 seconds finds a
  fraction of what is there.
- **Do not lower `MaxWait`/MX below 2 seconds** to make a scan feel faster — it just loses devices.
- **Expect `ssdp:all` to be noisy** — a single device answers once per advertisement
  (`3 + 2×embedded + services` datagrams). Use `SsdpConstants.RootDevice` when you want one hit
  per device.
- **Do not fetch a UPnP description on every `Found`.** Re-announcements are frequent; fetch once
  and re-fetch only when `ConfigId` changes.
- **Do not relax the description URL policy without reason.** The default exists because
  `LOCATION` is an unauthenticated URL from an unauthenticated datagram.
- On Android, never write `MulticastLock` code — it is handled internally.

## Things That Do NOT Exist

Do not generate these — they are not part of the API:

**mDNS**
- `IMdnsManager.CurrentAccess` / `RequestAccess` — there is no queryable mDNS permission on any
  platform. iOS prompts implicitly on first use.
- `MdnsBrowseStatus.Updated` — only `Found` and `Lost` exist; a changed service re-emits `Found`.
- Subtype browsing (`_printer._sub._http._tcp`).
- Domains other than `local`.

**SSDP / UPnP**
- **SOAP action invocation.** There is no `UpnpService.Invoke(...)`, no action argument
  marshalling, and no SCPD action metadata. `UpnpService.ControlUrl` is surfaced so you can drive
  your own SOAP client.
- **GENA eventing.** No `Subscribe`, no callback listener, no state-variable notifications.
  `UpnpService.EventSubscriptionUrl` is surfaced and nothing more.
- A native/OS-backed SSDP path on iOS or Android — there is none, on any platform.
- `SsdpBrowseConfig.DescriptionUrlFilter` — the filter is a parameter of `GetDescription`, not
  of the browse config.

**WS-Discovery**
- Device metadata exchange (WS-MetadataExchange / `GetMetadata`), and any ONVIF operation
  (`GetCapabilities`, `GetProfiles`, stream URIs). Discovery only.
- The `ldap` scope MatchBy rule — it is recognised and deliberately never matches.
- A `WsDiscoveryProfile` auto-negotiation mode; both versions are simply sent as two datagrams.
