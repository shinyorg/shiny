---
name: shiny-discovery
description: Generate code using Shiny.Net.Discovery for cross-platform mDNS/DNS-SD (Bonjour/Zeroconf) service browsing, resolution, and publishing on iOS, Android, Mac Catalyst, macOS, Windows, and Linux
auto_invoke: true
triggers:
  - mDNS
  - DNS-SD
  - Bonjour
  - Zeroconf
  - service discovery
  - network discovery
  - local network discovery
  - discover devices on the network
  - find devices on LAN
  - advertise a service
  - publish a service
  - IMdnsManager
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

You are an expert in Shiny.Net.Discovery, a cross-platform mDNS/DNS-SD (Bonjour/Zeroconf) library for
browsing, resolving, and publishing services on the local network.

## When to Use This Skill

Invoke this skill when the user wants to:
- Discover services or devices on the local network (printers, Chromecasts, IoT devices, peer apps)
- Advertise their own app as a discoverable service
- Read TXT record metadata from a discovered service
- Resolve a known service instance to a host, port, and IP addresses
- Ask about Bonjour/mDNS on iOS without the multicast entitlement
- Register mDNS in DI

## Library Overview

| Item       | Value                                                                                     |
|------------|-------------------------------------------------------------------------------------------|
| GitHub     | https://github.com/shinyorg/shiny                                                          |
| NuGet      | `Shiny.Net.Discovery`                                                                      |
| Namespace  | `Shiny.Net.Discovery` (types); `Shiny` (registration extensions)                            |
| Platforms  | iOS, Mac Catalyst, macOS, Android, Windows, Linux, server .NET                              |

### How each platform is backed

| Platform                     | Implementation                | Why                                                       |
|------------------------------|-------------------------------|-----------------------------------------------------------|
| iOS / Mac Catalyst / macOS   | `NSNetService` (Bonjour)      | Goes through the system mDNSResponder, so **no** `com.apple.developer.networking.multicast` entitlement |
| Android                      | `NsdManager`                  | No `CHANGE_WIFI_MULTICAST_STATE` and no `WifiManager.MulticastLock` |
| Windows / Linux / server .NET| Managed responder on UDP 5353 | No OS DNS-SD API to lean on; dependency-free, AOT-safe     |

**Do not tell users they need the iOS multicast entitlement.** That requirement applies to raw
multicast sockets, which this library deliberately avoids on Apple platforms.

## Setup

```csharp
using Shiny;

builder.Services.AddMdns();
```

Then inject `IMdnsManager`.

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

## API Reference

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

## Models

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

## Things That Do NOT Exist

Do not generate these — they are not part of the API:
- `IMdnsManager.CurrentAccess` / `RequestAccess` — there is no queryable mDNS permission on any
  platform. iOS prompts implicitly on first use.
- `MdnsBrowseStatus.Updated` — only `Found` and `Lost` exist; a changed service re-emits `Found`.
- Subtype browsing (`_printer._sub._http._tcp`).
- Domains other than `local`.
