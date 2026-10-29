---
name: shiny-nfc
description: Generate code using Shiny.Nfc to read and write NDEF NFC tags (URLs, text, MIME, smart posters, Android app records) on Android, iOS, Windows / macOS / Linux PC/SC readers and Blazor WebAssembly (Web NFC)
auto_invoke: true
triggers:
  - Shiny.Nfc
  - INfcManager
  - INfcTag
  - NdefMessage
  - NdefRecord
  - NdefTypeNameFormat
  - NdefText
  - NdefSmartPoster
  - NfcCapabilities
  - NfcScanOptions
  - NfcReadResult
  - NfcException
  - NfcTagLostException
  - NfcTagReadOnlyException
  - NdefFormatException
  - NdefMessageTooLargeException
  - NfcNotSupportedException
  - AddNfc
  - AddNotSupportedNfc
  - ReadOne
  - WriteOne
  - nfc
  - ndef
  - nfc tag
  - ntag
  - nfc sticker
  - CoreNFC
  - NFCNDEFReaderSession
  - NfcAdapter
  - reader mode
  - Web NFC
  - NDEFReader
  - PC/SC
  - pcsc
  - ACR122U
  - NFCReaderUsageDescription
---

# Shiny NFC Skill

You are an expert in Shiny.Nfc, a .NET library that reads and writes **NDEF** NFC tags through one `INfcManager`.

## When to Use This Skill

Invoke this skill when the user wants to:
- Read a URL, text or custom payload from an NFC tag or sticker
- Write a message to a tag, or lock it permanently
- Use an NFC reader (ACR122U and other PC/SC readers) from a Windows, macOS or Linux app
- Use Web NFC from Blazor WebAssembly

**NDEF only.** Payment cards (EMV), passports, MIFARE Classic, raw tag commands (`transceive`, APDUs), host card
emulation and peer-to-peer are out of scope - say so rather than generating code for them.

## Library Overview

**Documentation**: https://shinylib.net/client/nfc/
**Package**: `Shiny.Nfc`
**Namespaces**: `Shiny.Nfc` (types), `Shiny` (registration)

| Platform | Backend | Notes |
|---|---|---|
| Android | `NfcAdapter` reader mode on the current activity | UID available; blank tags are formatted on first write |
| iOS | CoreNFC `NFCNDEFReaderSession` | system scan sheet; **no UID** (`Identifier` is null); iPhone 7+ |
| Windows, macOS, Linux | PC/SC contactless reader (`winscard.dll`, `PCSC.framework`, `pcsc-lite`) | NFC Forum Type 2 (NTAG, Ultralight) and Type 4; MIFARE Classic reads as no NDEF |
| Blazor WebAssembly | Web NFC `NDEFReader` | Chrome on Android, secure context, user gesture |
| Mac Catalyst, iOS simulator | - | `Capabilities == None` |

## Registration

```csharp
using Shiny;

builder.Services.AddNfc();
```

### Required setup - always generate it

- **Android** (`Platforms/Android/AndroidManifest.xml`):
  ```xml
  <uses-permission android:name="android.permission.NFC" />
  <uses-feature android:name="android.hardware.nfc" android:required="false" />
  ```
- **iOS**: `NFCReaderUsageDescription` in Info.plist, and the entitlement with **`TAG`** (the App Store rejects `NDEF`
  since the iOS 18 SDK; `TAG` covers NDEF sessions). Enable "NFC Tag Reading" on the App ID.
  ```xml
  <!-- Info.plist -->
  <key>NFCReaderUsageDescription</key>
  <string>Reads and writes your NFC tags</string>
  <!-- Entitlements.plist -->
  <key>com.apple.developer.nfc.readersession.formats</key>
  <array><string>TAG</string></array>
  ```
- **Linux**: `pcscd` running and `libpcsclite1` installed, plus the reader's CCID driver (`pcsc_scan` to check).
- **Windows / macOS**: nothing - plug in a PC/SC reader.

## API

```csharp
namespace Shiny.Nfc;

public interface INfcManager
{
    NfcCapabilities Capabilities { get; }                    // None, ReadNdef, WriteNdef, MakeReadOnly, TagIdentifier (flags)
    Task<AccessState> RequestAccess(CancellationToken ct = default);
    IAsyncEnumerable<INfcTag> Scan(NfcScanOptions? options = null, CancellationToken ct = default);
}

public interface INfcTag
{
    byte[]? Identifier { get; }      // UID; null on iOS
    bool IsWritable { get; }
    int? MaxSize { get; }            // encoded bytes; null when unknown
    Task<NdefMessage?> ReadNdef(CancellationToken ct = default);   // null = blank / no message
    Task WriteNdef(NdefMessage message, CancellationToken ct = default);
    Task MakeReadOnly(CancellationToken ct = default);             // PERMANENT
}

public class NfcScanOptions { public string? AlertMessage { get; set; } }   // iOS scan sheet text

// one-tap helpers (NfcManagerExtensions)
Task<NfcReadResult?> ReadOne(this INfcManager, NfcScanOptions? options = null, CancellationToken ct = default);   // null = scan ended first
Task<bool> WriteOne(this INfcManager, NdefMessage message, NfcScanOptions? options = null, CancellationToken ct = default);
public record NfcReadResult(byte[]? Identifier, NdefMessage? Message);

public sealed class NdefMessage
{
    public NdefMessage(params IEnumerable<NdefRecord> records);
    public IReadOnlyList<NdefRecord> Records { get; }
    public int Length { get; }                                    // encoded size - compare with tag.MaxSize
    public byte[] ToByteArray();
    public static NdefMessage Parse(ReadOnlySpan<byte> data);
    public static bool TryParse(ReadOnlySpan<byte> data, out NdefMessage? message);
}

public sealed class NdefRecord
{
    public static NdefRecord CreateUri(string uri);                // well-known U, prefix-compressed
    public static NdefRecord CreateText(string text, string languageCode = "en", bool utf16 = false);
    public static NdefRecord CreateMime(string mimeType, ReadOnlyMemory<byte> data);
    public static NdefRecord CreateExternal(string domain, string type, ReadOnlyMemory<byte> data);
    public static NdefRecord CreateAndroidApplication(string packageName);   // AAR - opens/installs the app
    public static NdefRecord CreateSmartPoster(string uri, string? title = null, string languageCode = "en");
    public static NdefRecord CreateAbsoluteUri(string uri);
    public NdefRecord(NdefTypeNameFormat tnf, ReadOnlyMemory<byte> type, ReadOnlyMemory<byte> payload, ReadOnlyMemory<byte> id = default);

    public bool TryGetUri(out string? uri);
    public bool TryGetText(out NdefText? text);                   // NdefText(Text, LanguageCode)
    public bool TryGetSmartPoster(out NdefSmartPoster? poster);   // NdefSmartPoster(Uri, Titles)
    public string? MimeType { get; }
    public NdefTypeNameFormat TypeNameFormat { get; }
    public ReadOnlyMemory<byte> Type { get; }
    public ReadOnlyMemory<byte> Payload { get; }
}
```

Exceptions (all `NfcException`): `NfcTagLostException` (tag left the field), `NfcTagReadOnlyException`,
`NdefMessageTooLargeException` (nothing written), `NdefFormatException` (invalid bytes on the tag),
`NfcNotSupportedException` (platform / tag cannot do it - check `Capabilities` first).

## Code Generation Instructions

1. Add `using Shiny.Nfc;` where `INfcManager` is injected and `using Shiny;` where `AddNfc()` is called.
2. **Always emit the platform setup above** for every platform the app targets.
3. **A tag is only valid inside the `await foreach` body that received it.** Never store an `INfcTag`, return it from a
   method, or use it after `break`. Keep `tag.Identifier` or the `NdefMessage` instead. Leaving the loop ends the session
   (dismisses the iOS sheet, stops reader mode).
4. For a single read or write, use `ReadOne()` / `WriteOne(message)` - they do the work inside the loop.
5. Only one scan at a time; a second concurrent `Scan` throws `InvalidOperationException`. Pass a `CancellationToken`
   (or `break`) to stop a continuous scan.
6. Check `Capabilities` before showing NFC UI: `None` → hide it. Check `RequestAccess()` for `Disabled` (Android: NFC
   off - send the user to Settings) and `NotSetup` (PC/SC: no reader plugged in yet).
7. Catch `NfcTagLostException` around reads and writes and ask the user to hold the tag still. A write interrupted by a
   lost tag may have destroyed the previous message.
8. Check `message.Length <= tag.MaxSize` (when `MaxSize` is not null) before writing, or catch
   `NdefMessageTooLargeException`.
9. `ReadNdef()` returning `null` means a blank tag - not an error.
10. **Never call `MakeReadOnly()` without explicit user confirmation** - it can never be undone.
11. iOS: set `NfcScanOptions.AlertMessage` ("Hold your iPhone near the tag"); the user dismissing the sheet ends the loop
    normally (`ReadOne` returns null). Don't generate UID-based logic for iOS - `Identifier` is null there.
12. Blazor: start the scan from a button click (Web NFC needs a user gesture); a write goes to the next tag in range.
13. Never use a tag UID as authentication - it is not secret and some phones randomise it.

```csharp
public class TagViewModel(INfcManager nfc)
{
    public async Task<string?> ReadUrl(CancellationToken ct)
    {
        if (nfc.Capabilities == NfcCapabilities.None)
            return null;

        var result = await nfc.ReadOne(new NfcScanOptions { AlertMessage = "Hold your phone near the tag" }, ct);
        var record = result?.Message?.Records.FirstOrDefault();
        return record != null && record.TryGetUri(out var uri) ? uri : null;
    }

    public async Task<bool> WriteUrl(string url, CancellationToken ct)
    {
        var message = new NdefMessage(NdefRecord.CreateUri(url));
        try
        {
            return await nfc.WriteOne(message, ct: ct);
        }
        catch (NfcTagLostException)
        {
            return false;   // ask the user to hold the tag still and try again
        }
    }

    // continuous scanning - e.g. a check-in kiosk with a PC/SC reader
    public async Task CheckIn(Func<string, Task> onTag, CancellationToken ct)
    {
        await foreach (var tag in nfc.Scan(ct: ct))
        {
            if (tag.Identifier is { } uid)
                await onTag(Convert.ToHexString(uid));
        }
    }
}
```
