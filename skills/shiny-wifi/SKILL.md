---
name: shiny-wifi
description: Generate code using Shiny.Net.Wifi for cross-platform Wi-Fi - scanning for access points, connecting and disconnecting, listing/forgetting/rejoining the networks the device has saved, monitoring the current network (SSID, signal, IP, DNS), and hosting a hotspot with connected-client listing on Android, iOS, Mac Catalyst, macOS, Windows, and Linux
auto_invoke: true
triggers:
  - wifi
  - Wi-Fi
  - WiFi
  - wireless network
  - scan for wifi
  - wifi scanner
  - list wifi networks
  - nearby networks
  - access point
  - SSID
  - BSSID
  - RSSI
  - GetCurrentNetwork
  - current network
  - which wifi am I on
  - ssid is null
  - bssid is null
  - unknown ssid
  - NEHotspotNetwork
  - fetchCurrent
  - CNCopyCurrentNetworkInfo
  - FLAG_INCLUDE_LOCATION_INFO
  - signal strength
  - connect to wifi
  - join a wifi network
  - disconnect wifi
  - wifi password
  - passphrase
  - WPA2
  - WPA3
  - hotspot
  - mobile hotspot
  - tethering
  - local only hotspot
  - LocalOnlyHotspot
  - soft AP
  - access point mode
  - hotspot clients
  - who is connected to my hotspot
  - known networks
  - saved networks
  - saved wifi profiles
  - remembered networks
  - forget a wifi network
  - remove a saved network
  - reconnect to a saved network
  - rejoin a known network
  - list configured SSIDs
  - getConfiguredSSIDs
  - network suggestion
  - toggle wifi
  - turn wifi on
  - wifi radio
  - current network
  - what network am I on
  - my IP address
  - DNS servers
  - gateway
  - subnet mask
  - IWifiManager
  - IWifiHotspot
  - IHotspotSession
  - WifiNetwork
  - KnownWifiNetwork
  - GetKnownNetworks
  - WifiNetworkInfo
  - WifiConnectionRequest
  - WifiCapabilities
  - WifiSecurity
  - WifiBand
  - HotspotConfiguration
  - HotspotInfo
  - HotspotClient
  - WifiNotSupportedException
  - WifiPermissionException
  - WifiConnectionException
  - AddWifi
  - AddWifiHotspot
  - Shiny.Net.Wifi
  - NEHotspotConfiguration
  - NEHotspotConfigurationManager
  - CaptiveNetwork
  - CoreWLAN
  - CWInterface
  - CWNetworkProfile
  - WifiNetworkSpecifier
  - WifiNetworkSuggestion
  - wlanapi
  - WlanGetProfileList
  - NEARBY_WIFI_DEVICES
  - wiFiControl
  - NetworkOperatorTetheringManager
  - NetworkManager AP mode
---

# Shiny.Net.Wifi Skill

You are an expert in Shiny.Net.Wifi, a cross-platform Wi-Fi library covering **scanning**,
**connect/disconnect**, **saved (known) network management**, **current-network monitoring** and
**hotspot hosting**.

## When to Use This Skill

Invoke this skill when the user wants to:
- List the Wi-Fi networks in range, with signal strength and security
- Join or leave a named network from code
- Read or watch the current network - SSID, BSSID, signal, IP, DNS, gateway, mask
- List, forget, or rejoin the networks the device has saved
- Raise a hotspot / access point and show the user its SSID and passphrase
- See which devices are connected to a hotspot
- Power the Wi-Fi radio on or off
- Ask why Wi-Fi scanning "does not work on iOS"

## ⚠️ Read this before writing anything

**Wi-Fi is the most unevenly exposed capability across these platforms.** Half of what users ask for
is impossible on at least one of them, and the impossibility is a platform policy decision, not a
gap in this library. Do **not** write code that assumes an operation exists everywhere, and do not
tell a user something will work on iOS when it will not.

Every manager publishes a `WifiCapabilities` flags property. **Branch on it.** Anything unavailable
throws `WifiNotSupportedException` with a message naming the exact limit.

## Capability matrix

| Operation | Android | iOS / Mac Catalyst | macOS | Windows | Linux |
|-----------|---------|--------------------|-------|---------|-------|
| Scan | ✅ (needs location) | ❌ **no API** | ✅ | ✅ | ✅ |
| Connect | ✅ (system dialog from API 29) | ✅ (system dialog) | ✅ | ✅ | ✅ |
| Disconnect | ✅ | ✅ (removes the config) | ✅ | ✅ | ✅ |
| Current network SSID | ✅ (needs location) | ✅ (needs entitlement + location) | ✅ (needs location) | ✅ | ✅ |
| IP / DNS / gateway | ✅ | ✅ | ✅ | ✅ | ✅ |
| Radio state | ✅ | ❌ | ✅ | ✅ | ✅ |
| Radio toggle | ⚠️ API ≤ 28 only | ❌ | ✅ | ✅ | ✅ |
| Hotspot | ⚠️ local-only, OS picks SSID | ❌ **no API** | ❌ **no API** | ✅ full tethering | ✅ AP mode |
| Hotspot clients | ❌ | ❌ | ❌ | ✅ | ✅ |
| List known networks | ⚠️ own app only | ⚠️ own app only | ✅ whole machine | ✅ whole machine | ✅ whole machine |
| Forget a known network | ⚠️ own app only | ⚠️ own app only | ⚠️ needs admin auth | ✅ | ✅ (polkit) |
| Connect by known id | ⚠️ API ≤ 28 only | ❌ | ✅ | ✅ | ✅ |

### The three things users most often ask for that cannot be done

1. **Scanning on iOS.** There is no public API. `NEHotspotHelper` can list networks but its
   entitlement is granted case by case by Apple to captive-network-assistant apps. Do not suggest
   `CNCopyCurrentNetworkInfo` or `NEHotspotNetwork.fetchCurrent` as a substitute - both report the
   *joined* network, not nearby ones.
2. **Reading the user's saved networks on a phone.** Neither iOS nor Android will show an app the
   networks the *user* saved. `GetKnownNetworks()` on those two returns only what **your own app**
   configured. Do not build a "manage all my Wi-Fi networks" screen for mobile.
3. **Naming an Android hotspot.** `SoftApConfiguration.Builder` exposes only the channel to
   non-system apps - `setSsid`/`setPassphrase` are `@SystemApi`. The OS generates both and you read
   them back off `IHotspotSession.Info` to show the user.

## Library Overview

| Item       | Value                                                                          |
|------------|--------------------------------------------------------------------------------|
| GitHub     | https://github.com/shinyorg/shiny                                              |
| NuGet      | `Shiny.Net.Wifi`, plus `Shiny.Net.Wifi.Linux` on Linux                          |
| Namespace  | `Shiny.Net.Wifi` (types); `Shiny` (registration extensions)                      |
| Platforms  | Android, iOS, Mac Catalyst, macOS, Windows, Linux                               |

### How each platform is backed

| Platform | Manager | Known networks | Hotspot |
|----------|---------|----------------|---------|
| Android | `WifiManager` + `ConnectivityManager` | `WifiNetworkSuggestion` (API 30+), `WifiConfiguration` below 29 | `startLocalOnlyHotspot` |
| iOS / Mac Catalyst | `NEHotspotConfiguration` + `CaptiveNetwork` | `NEHotspotConfigurationManager.getConfiguredSSIDs` | none |
| macOS | CoreWLAN (`CWInterface`) | `CWConfiguration.networkProfiles` | none |
| Windows | `WiFiAdapter` (WinRT) | `wlanapi.dll` - WinRT has no profile API | `NetworkOperatorTetheringManager` |
| Linux | NetworkManager / D-Bus | `Settings.ListConnections` (UUID-keyed) | NetworkManager AP mode + `ipv4.method=shared` |
| plain .NET | `System.Net.NetworkInformation` (addressing only) | none | none |

**On Linux, reference `Shiny.Net.Wifi.Linux` instead of the base package.** It registers
NetworkManager-backed implementations of the same interfaces. The base package's plain .NET
target reports IP/DNS off the wireless interface and raises `Changed`, but every Wi-Fi-specific call
throws - it is a deliberate stub, not a fallback.

## Registration

```csharp
builder.Services.AddWifi();            // IWifiManager
builder.Services.AddWifiHotspot();     // IWifiHotspot
```

Both are singletons. Register only what you use - each one costs a native watcher only once
something subscribes to its `Changed` event.

## Scanning

```csharp
public class NetworkPicker(IWifiManager wifi)
{
    public async Task<IReadOnlyList<WifiNetwork>> Load(CancellationToken ct)
    {
        if (!wifi.Capabilities.HasFlag(WifiCapabilities.Scan))
            return [];   // iOS - show a "join by name" field instead

        var access = await wifi.RequestAccess(ct);
        if (access != AccessState.Available)
            throw new InvalidOperationException("Location access is needed to scan for networks");

        var found = await wifi.Scan(ct);

        // one SSID appears once per radio on a multi-band or mesh network, so results are unique
        // on BSSID - group and take the strongest if you are building a picker
        return found
            .GroupBy(x => x.Ssid)
            .Select(g => g.MaxBy(x => x.SignalStrengthPercent)!)
            .Where(x => !x.IsHidden)
            .ToList();
    }
}
```

`WifiNetwork` carries `Ssid`, `Bssid`, `Security`, `SignalStrengthDbm` (null where the platform
reports only a percentage), `SignalStrengthPercent` (0-100, always populated), `FrequencyMhz`,
`IsHidden`, and computed `Band` / `Channel` / `IsOpen`.

**Always call `RequestAccess` before `Scan`.** Android returns an empty list rather than an error
when location has not been granted; the library turns that into `WifiPermissionException` so it
does not look like an empty neighbourhood, but asking first is better than catching.

## Connecting

```csharp
var request = new WifiConnectionRequest("Kitchen")
{
    Passphrase = "hunter2hunter2",
    Remember = true,                       // save the profile for automatic rejoin
    Timeout = TimeSpan.FromSeconds(20)
};

try
{
    var joined = await wifi.Connect(request, ct);
    logger.LogInformation("On {Ssid} at {Ip}", joined.Ssid, joined.IPv4Address);
}
catch (WifiConnectionException ex)
{
    // wrong passphrase, out of range, the user declined the system prompt, or DHCP timed out
}
```

`Connect` returns only once an address has been assigned, not when association completes - a
`WifiNetworkInfo` with no IP on it is useless to the caller.

**Leave `Security` as `Unknown` unless you are joining a hidden network.** The platform reads the
scheme off the beacon; a hidden network has no beacon to read, so it has to be told.

**Android 10+ and iOS both show a system dialog naming the network.** Neither lets an app join
silently, and on Android the join itself lasts only while your app holds the request. `Remember`
still does something everywhere: it writes an ordinary profile on Windows, macOS and Linux, keeps
the hotspot configuration on iOS, and on Android 11+ registers a `WifiNetworkSuggestion` next to
the join so the OS can come back to the network later. See **Known (saved) networks** below.

## Known (saved) networks

```csharp
if (wifi.Capabilities.HasFlag(WifiCapabilities.KnownNetworks))
{
    foreach (var known in await wifi.GetKnownNetworks(ct))
        Console.WriteLine($"{known.Ssid} ({known.Security}) id={known.Id}");
}

// forget one - safe to call even if it was never saved
if (wifi.Capabilities.HasFlag(WifiCapabilities.ForgetNetwork))
    await wifi.Forget(known.Id, ct);

// rejoin one without handing the passphrase over again
if (wifi.Capabilities.HasFlag(WifiCapabilities.ConnectKnownNetwork))
    await wifi.Connect(known.Id, ct);
```

`KnownWifiNetwork` carries `Id`, `Ssid`, `Security`, `IsHidden` and `AddedByThisApp`.

- **`Id` is opaque and platform-issued.** A NetworkManager connection UUID on Linux, a numeric
  network id on Android below API 29, the SSID everywhere else. Round-trip it; never parse it,
  construct it, or persist it across platforms. Match on `Ssid` if you need to find a network by
  name.
- **The scope of "known" is not the same everywhere, and this is the thing to get right.** iOS,
  Mac Catalyst and Android disclose **only your own app's** entries -
  `NEHotspotConfigurationManager.getConfiguredSSIDs` and network suggestions respectively. Windows,
  macOS and Linux hand back every profile on the machine. `AddedByThisApp` tells the two apart; it
  is false on the desktop platforms even for profiles your app created, because none of them record
  who wrote an entry.
- **`Connect(id)` is desktop-plus-legacy-Android only.** On iOS and Android 10+ a saved network is
  a standing hint the OS acts on when it chooses - there is no call to force the join. Use
  `Connect(WifiConnectionRequest)` with the passphrase there instead.
- **Getting something *into* the list means `Remember = true` on the join.** On Android 11+ that
  registers a `WifiNetworkSuggestion` alongside the specifier join; the specifier is still what
  gets the device on the network now, and the suggestion only takes effect once the user approves
  the notification Android raises.
- **iOS reports names only.** A stored hotspot configuration carries no security type or hidden
  flag, so those stay at their defaults there.
- **macOS `Forget` usually throws.** Editing the preferred-network list means committing a whole
  `CWConfiguration`, which macOS gates behind an `SFAuthorization` a normal app cannot raise -
  expect `WifiPermissionException` and have a fallback. Listing is unprivileged.
- **`GetKnownNetworks()` is not free on desktop.** Windows reads one profile's XML per entry and
  Linux makes one D-Bus round trip per profile, so cache the result rather than polling it.

## Current network and change monitoring

```csharp
public sealed class NetworkWatcher(IWifiManager wifi) : IDisposable
{
    public void Start() => wifi.Changed += this.OnChanged;
    public void Dispose() => wifi.Changed -= this.OnChanged;

    void OnChanged(object? sender, WifiNetworkInfo? network)
    {
        if (network == null)
        {
            // dropped off Wi-Fi entirely
            return;
        }
        Console.WriteLine($"{network.Ssid} ({network.SignalStrengthPercent}%) {network.IPv4Address}");
        Console.WriteLine($"DNS: {String.Join(", ", network.DnsAddresses)}");
    }
}
```

`WifiNetworkInfo` carries `Ssid`, `Bssid`, `Security`, `SignalStrengthDbm`,
`SignalStrengthPercent`, `FrequencyMhz`, `Band`, `Channel`, `IpAddresses`, `DnsAddresses`,
`Gateway`, `SubnetMask`, `InterfaceName`, and the `IPv4Address` / `IPv6Address` shortcuts.

- **`Changed` is de-duplicated.** The native watchers behind it (Android's `NetworkCallback`,
  Apple's `NWPathMonitor`, NetworkManager's `PropertiesChanged`) all fire several times per real
  change; only genuine differences are raised. `WifiNetworkInfo` compares its address lists by
  value for the same reason, so it is safe to diff yourself too.
- **Unsubscribe.** The native watcher is created on the first subscription and torn down on the
  last, so a leaked handler keeps a radio callback alive.
- **Subscribing delivers the current network once**, then only real changes after that. A new
  subscriber does not have to seed itself with a separate read.
- **`GetCurrentNetwork(ct)` is async and reads live on every call.** Hold the result rather than
  re-reading it in a loop. There is no `CurrentNetwork` property - it was removed because the two
  mobile platforms stopped answering synchronously (see below).
- **Addressing is available everywhere; the SSID is not.** IP/DNS/gateway come from the managed
  network stack, so they need no permission. Individual fields are still best-effort - a platform
  may not implement one (`GatewayAddresses` is unsupported on Android) and a refused field comes
  back null or empty rather than throwing. `Ssid` and `Bssid` need `WifiCapabilities.CurrentNetwork`
  and the platform permission behind it, and come back null otherwise.

### Why reading the SSID is asynchronous

Both mobile platforms removed the synchronous answer, and on both the failure is silent - the call
succeeds and the SSID is simply null:

- **iOS 14+** - `CNCopyCurrentNetworkInfo` returns nothing unless your own app configured the
  network being asked about. The replacement, `NEHotspotNetwork.fetchCurrent`, is async-only.
  Shiny uses it, so iOS now also reports `Security` and `SignalStrengthPercent`, which
  CaptiveNetwork never did.
- **Android 12 (API 31)+** - the SSID and BSSID are redacted out of *every* pull-style read
  (`getConnectionInfo`, and the `WifiInfo` off `getNetworkCapabilities`) no matter what permissions
  are held. Only a `NetworkCallback` registered with `FLAG_INCLUDE_LOCATION_INFO` gets them, and
  that is push-based. Shiny registers one and serves reads from it.

**If a user reports a null or `<unknown ssid>` SSID with permissions granted, this is why** - check
they are on a Shiny version with `GetCurrentNetwork` rather than telling them to add permissions.

## Hotspot

```csharp
if (!hotspot.IsSupported)
    return;   // iOS and macOS

await using var session = await hotspot.Start(
    new HotspotConfiguration { Ssid = "shiny-setup", Passphrase = "letmein12345" },
    ct
);

// ALWAYS read the settings back - Android ignores what you asked for and picks its own
ShowToUser(session.Info.Ssid, session.Info.Passphrase);

if (wifi.Capabilities.HasFlag(WifiCapabilities.HotspotClients))
{
    foreach (var client in await session.GetClients(ct))
        Console.WriteLine($"{client.MacAddress} {client.IpAddress}");
}
```

- **The session is the hotspot's lifetime.** Disposing it (or calling `Stop`) brings the access
  point down. Android in particular tears its reservation down when the owning process exits.
- **`HotspotCustomConfiguration`** tells you whether the SSID/passphrase you passed were honoured.
  Windows and Linux honour them; Android does not.
- **`GetClients()` is a snapshot, not a subscription** - poll it for a live count. A client appears
  once it has taken a DHCP lease, not the instant it associates. On Linux the list comes from the
  kernel neighbour table, so entries linger for a minute or so after a device leaves. Android
  throws - it has no client list and blocked the ARP table apps used to read in Android 10.
- **Android's hotspot is local-only**: clients reach the device but get no internet. Windows shares
  the machine's existing internet connection, and fails if there is none to share. Linux runs
  NetworkManager AP mode with DHCP and NAT.
- **Raising a hotspot usually takes the radio out of station mode**, dropping the device off any
  network it was joined to.

## Radio power

```csharp
if (wifi.Capabilities.HasFlag(WifiCapabilities.RadioToggle))
    await wifi.SetRadioEnabled(true, ct);
```

Android revoked `setWifiEnabled` for third-party apps in API 29 - the capability flag is only set
below that. Send the user to `Settings.Panel.ACTION_WIFI` instead. iOS never allowed it.

## Platform setup

### Android — `AndroidManifest.xml`

```xml
<uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
<uses-permission android:name="android.permission.CHANGE_WIFI_STATE" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.NEARBY_WIFI_DEVICES"
                 android:usesPermissionFlags="neverForLocation"
                 tools:targetApi="33" />
```

`ACCESS_FINE_LOCATION` is not optional even on API 33+: `NEARBY_WIFI_DEVICES` unlocks scanning, but
the SSID of the joined network still needs location.

### iOS / Mac Catalyst

- App ID capabilities: **Hotspot Configuration** and **Access WiFi Information**
- `Info.plist`: `NSLocationWhenInUseUsageDescription`

Without the location grant, iOS 13+ returns a placeholder ("Wi-Fi", "WLAN") for the SSID rather
than failing - which is why `RequestAccess` asks for location.

### macOS

- `Info.plist`: `NSLocationWhenInUseUsageDescription` (Sonoma and later gate SSID and scan results
  on it)
- Sandboxed apps: `com.apple.security.network.client`

### Windows — `Package.appxmanifest`

```xml
<DeviceCapability Name="wiFiControl" />
<DeviceCapability Name="radios" />
```

### Linux

Requires a running NetworkManager - the default on Ubuntu, Fedora, Debian desktop and Raspberry Pi
OS, but *not* on a systemd-networkd or netplan-only server. Reading and scanning are unprivileged.
Connecting, disconnecting, hotspot and radio toggling go through polkit: interactive on a desktop,
and in a headless session needing rules for
`org.freedesktop.NetworkManager.network-control` and `…enable-disable-wifi`.

## Exceptions

| Exception | Means | Recoverable |
|-----------|-------|-------------|
| `WifiNotSupportedException` | The OS has no API for this. The message names the limit. | No - branch on `Capabilities` instead |
| `WifiPermissionException` | A permission, entitlement or manifest entry is missing. The message names it. | Yes - fix the manifest or call `RequestAccess` |
| `WifiConnectionException` | The join failed: wrong passphrase, out of range, user declined, DHCP timeout | Yes - retry or re-prompt |
| `WifiException` | The base. Catch this for anything Wi-Fi. | Depends |

## Code Generation Rules

1. **Check `Capabilities` before offering a feature in UI.** Catching
   `WifiNotSupportedException` is the backstop, not the plan.
2. **Call `RequestAccess` before `Scan` or before `GetCurrentNetwork()`** if you need the SSID.
   Without it the call still succeeds and `Ssid`/`Bssid` come back null.
3. **Never claim scanning works on iOS.** Offer a "join by name" field there instead.
4. **Never present `GetKnownNetworks()` on iOS or Android as "the device's saved networks".** It is
   your app's own entries only - say so in the UI, or the list looks broken when it comes back empty.
5. **Always read `IHotspotSession.Info` back** rather than assuming the requested SSID was used.
6. **Dispose the hotspot session** - `await using` is the idiomatic form.
7. **Unsubscribe from `Changed`** in `Dispose`; the native watcher is reference-counted.
8. **Group scan results by SSID** when building a picker - one network yields several BSSIDs.
9. **Do not treat `SignalStrengthDbm` as always present.** Linux and iOS report only a percentage.
10. **Never write `wifi.CurrentNetwork`** - it does not exist. It is `await wifi.GetCurrentNetwork()`,
    and the `Changed` handler already receives the new `WifiNetworkInfo?` so it never needs to re-read.
11. **Handle `WifiSecurity.Psk`.** iOS reports "personal" without naming the WPA generation, so a
    `switch` over `WifiSecurity` that only lists `Wpa2Psk`/`Wpa3Psk` will miss iOS entirely.
   `SignalStrengthPercent` is populated everywhere and is the safe one to display.
10. **On Linux, reference `Shiny.Net.Wifi.Linux`,** not just the base package.
