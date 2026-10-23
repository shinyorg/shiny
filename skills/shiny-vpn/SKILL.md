---
name: shiny-vpn
description: Generate code using Shiny.Net.Vpn to install, connect, disconnect and monitor a profile in the operating system's built-in IKEv2 VPN client (shared secret, username/password EAP, or client certificate) on iOS, Mac Catalyst, macOS, Android and Linux (NetworkManager + strongSwan)
auto_invoke: true
triggers:
  - vpn
  - VPN
  - IKEv2
  - IKE v2
  - IPsec
  - personal vpn
  - connect to vpn
  - start vpn
  - stop vpn
  - vpn status
  - vpn profile
  - vpn configuration
  - add vpn configuration
  - vpn shared secret
  - pre-shared key
  - PSK
  - EAP
  - EAP-MSCHAPv2
  - vpn username password
  - vpn certificate
  - p12
  - pkcs12
  - NEVPNManager
  - NEVpnManager
  - NEVPNProtocolIKEv2
  - NEVpnProtocolIke2
  - NEVPNStatus
  - Personal VPN entitlement
  - com.apple.developer.networking.vpn.api
  - allow-vpn
  - Ikev2VpnProfile
  - PlatformVpnProfile
  - VpnManager
  - provisionVpnProfile
  - startProvisionedVpnProfile
  - VpnProfileState
  - strongSwan
  - network-manager-strongswan
  - NetworkManager vpn
  - IVpnManager
  - VpnProfile
  - VpnAuthentication
  - SharedSecretAuthentication
  - UsernamePasswordAuthentication
  - CertificateAuthentication
  - VpnCapabilities
  - VpnConnectionState
  - VpnException
  - VpnNotSupportedException
  - VpnPermissionException
  - VpnConnectionException
  - AddVpn
  - Shiny.Net.Vpn
---

# Shiny.Net.Vpn Skill

You are an expert in Shiny.Net.Vpn, which installs and drives **one IKEv2 profile in the operating
system's own VPN client** - the same client the user's Settings app manages.

## When to Use This Skill

Invoke this skill when the user wants to:
- Add a VPN configuration to the device from their app (company VPN, a VPN service's own app)
- Connect or disconnect that VPN from code, and know whether it came up
- Show the VPN's state in their UI and react when the user toggles it in Settings
- Remove the VPN configuration again (sign-out, unenrolment)

## ⚠️ Read this before writing anything

**This is not a VPN protocol implementation.** It configures the OS's built-in **IKEv2** client.
It cannot do WireGuard, OpenVPN or a custom protocol - those need an Apple packet tunnel app
extension (`NEPacketTunnelProvider`) or an Android `VpnService` that implements the protocol and
moves every packet itself. If the user's server is WireGuard/OpenVPN, say so instead of writing
code that cannot work.

**One profile per app.** iOS's personal VPN and Android's provisioned profiles both allow exactly
one per app. `SaveProfile` replaces it; there is no list of profiles.

**The OS owns the tunnel.** It keeps running after the app is closed, appears in system VPN
settings, and the user can turn it off or delete it there - so read the state, never cache it.

Every manager publishes `VpnCapabilities`. **Branch on it.** Anything unavailable throws
`VpnNotSupportedException` naming the exact limit.

## Capability matrix

| Capability | iOS / Mac Catalyst / macOS | Android 13+ (API 33) | Android 11-12 (API 30-32) | Android < 11 | Linux |
|------------|---------------------------|----------------------|---------------------------|--------------|-------|
| `Profile` / `Connect` / `Disconnect` | ✅ | ✅ | ✅ | ❌ | ✅ |
| `Status` (state + `StatusChanged`) | ✅ | ✅ | ❌ | ❌ | ✅ |
| `SharedSecretAuthentication` | ✅ | ✅ | ✅ | ❌ | ✅ |
| `UsernamePasswordAuthentication` | ✅ | ✅ | ✅ | ❌ | ✅ |
| `CertificateAuthentication` | ✅ | ✅ | ✅ | ❌ | ❌ (plugin reads key files only) |
| `ServerRootCertificate` | ❌ (CA must come from a config profile) | ✅ | ✅ | ❌ | ✅ |
| `RemoteIdentifier` ≠ server address | ✅ | ❌ | ❌ | ❌ | ✅ |

Without `Status`, `Connect` returns as soon as the OS accepts the start and `GetStatus` returns
`VpnConnectionState.Unknown` - there is no way to know whether it came up.

## Library Overview

| Item       | Value |
|------------|-------|
| GitHub     | https://github.com/shinyorg/shiny |
| NuGet      | `Shiny.Net.Vpn`, plus `Shiny.Net.Vpn.Linux` on Linux |
| Namespace  | `Shiny.Net.Vpn` (types); `Shiny` (registration) |
| Platforms  | iOS, Mac Catalyst, macOS, Android, Linux. **No Windows** and no tvOS target |

| Platform | Backed by |
|----------|-----------|
| iOS / Mac Catalyst / macOS | `NEVPNManager.shared` + `NEVPNProtocolIKEv2`; secrets go into the app's keychain and are passed as persistent references |
| Android | `VpnManager.provisionVpnProfile(Ikev2VpnProfile)` - the OS's own client, **not** `VpnService` |
| Linux | NetworkManager over D-Bus, `vpn.service-type = org.freedesktop.NetworkManager.strongswan` |

## Registration

```csharp
builder.Services.AddVpn();   // IVpnManager, singleton
```

On Linux reference `Shiny.Net.Vpn.Linux` - it declares `AddVpn()`. The base package declares it only
on the platform targets, so the call never collides.

## Saving a profile

```csharp
public class VpnSetup(IVpnManager vpn)
{
    public async Task<bool> Install(string secret)
    {
        var result = await vpn.SaveProfile(new VpnProfile
        {
            Name = "Contoso VPN",                     // shown in Settings (Android shows the app name)
            ServerAddress = "vpn.contoso.com",
            LocalIdentifier = "phone@contoso.com",    // required on Android for PSK / certificate
            Authentication = VpnAuthentication.SharedSecret(secret)
        });
        return result == AccessState.Available;      // Denied = user declined the system prompt
    }
}
```

Other authentication kinds:

```csharp
VpnAuthentication.UsernamePassword("allan", password)          // IKEv2 EAP-MSCHAPv2
VpnAuthentication.Certificate(File.ReadAllBytes("me.p12"), "p12-password")
```

- The **first save shows an OS consent prompt** - call it from the foreground, in response to a user
  action. A decline returns `AccessState.Denied`; it does not throw.
- `RemoteIdentifier` defaults to `ServerAddress`. Only set it if the server's IKE ID differs - and
  that throws `VpnNotSupportedException` on Android.
- `ServerRootCertificate` (DER or PEM bytes) is for a server certificate issued by a private CA.
  It throws on Apple, where a private CA has to be installed through a configuration profile.
- Saving does not connect, and does not restart a running tunnel.

## Connecting and status

```csharp
try
{
    await vpn.Connect(TimeSpan.FromSeconds(20));    // waits until Connected
}
catch (VpnConnectionException ex)
{
    // ex.Message carries the OS's reason: authentication rejected, server not responding, ...
}
catch (VpnPermissionException)
{
    // no profile saved (or the user deleted it in Settings / withdrew consent) - call SaveProfile
}

await vpn.Disconnect();                             // waits briefly for the tunnel to go down
var state = await vpn.GetStatus();                  // VpnConnectionState
```

```csharp
vpn.StatusChanged += this.OnVpnStatus;             // delivers the current state on subscribe
void OnVpnStatus(object? sender, VpnConnectionState state) { /* update UI - may be off the main thread */ }
// unsubscribe in Dispose - the native watcher is reference counted
```

`VpnConnectionState`: `Unknown` (no `Status` capability), `NotConfigured`, `Disconnected`,
`Connecting`, `Connected`, `Reconnecting`, `Disconnecting`.

## Removing

```csharp
await vpn.RemoveProfile();   // disconnects, deletes the profile and its stored secrets; no-op if none
```

## Platform setup

### iOS / Mac Catalyst / macOS
- Enable the **Personal VPN** capability on the App ID.
- `Entitlements.plist`:
  ```xml
  <key>com.apple.developer.networking.vpn.api</key>
  <array><string>allow-vpn</string></array>
  ```
- Without it the save fails with the same NetworkExtension error a user decline produces, so it
  surfaces as `Denied`. A `Denied` on a first run that never showed a prompt means the entitlement is
  missing.

### Android
- No manifest entries. Needs API 30 for anything; API 33 for `Status`.
- The consent dialog comes through `AndroidPlatform.StartActivityForResult`, so the host must forward
  activity results (Shiny.Hosting.Maui does; Shiny.Hosting.Native through `ShinyAndroidActivity`).

### Linux
- `sudo apt install network-manager-strongswan` (Debian/Ubuntu/Raspberry Pi OS) or
  `NetworkManager-strongswan` (Fedora). `SaveProfile` throws naming the package when it is missing.
- Saving and connecting go through polkit (`org.freedesktop.NetworkManager.settings.modify.system`,
  `…network-control`): interactive on a desktop, a polkit rule for headless. A polkit refusal on save
  returns `AccessState.Denied`.
- The profile is the NetworkManager connection whose UUID is derived from the entry assembly name -
  renaming the app's assembly orphans the old one.

## Exceptions

| Exception | Means |
|-----------|-------|
| `VpnNotSupportedException` | The platform cannot do this (see the matrix). Branch on `Capabilities`. |
| `VpnPermissionException` | No profile saved, consent withdrawn, or polkit refused. |
| `VpnConnectionException` | The tunnel failed or timed out - the message carries the OS's reason. |
| `VpnException` | Base type; also storage/keychain/NetworkManager failures. |

## Code Generation Rules

1. **Never suggest this for WireGuard, OpenVPN or a custom protocol.** IKEv2 only.
2. **Check `Capabilities`** before offering certificate auth (not on Linux), a CA certificate (not on
   Apple) or a custom remote identity (not on Android).
3. **Call `SaveProfile` from the foreground** in response to a user action, and handle
   `AccessState.Denied` - never assume it saved.
4. **Never cache "connected".** The user can turn the VPN off in Settings; use `GetStatus()` or
   `StatusChanged`.
5. **Set `LocalIdentifier`** for shared-secret and certificate profiles if Android is a target.
6. **Do not store the secret yourself to "re-save" it** - the OS keeps it with the profile.
7. **Marshal `StatusChanged` to the UI thread** before touching UI.
8. **No Windows.** Do not write `AddVpn()` for a Windows head - there is no Windows target.
