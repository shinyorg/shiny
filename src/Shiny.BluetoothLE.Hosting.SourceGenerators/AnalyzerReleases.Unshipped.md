; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SBH001 | Shiny.BluetoothLE.Hosting | Error | BLE hosted type must be a top level non-generic partial class
SBH002 | Shiny.BluetoothLE.Hosting | Error | Invalid Bluetooth UUID
SBH003 | Shiny.BluetoothLE.Hosting | Error | Characteristic handler outside a BLE service
SBH004 | Shiny.BluetoothLE.Hosting | Error | Duplicate characteristic handler
SBH005 | Shiny.BluetoothLE.Hosting | Error | Request/response characteristic conflicts with a write or notify handler
SBH006 | Shiny.BluetoothLE.Hosting | Error | Unsupported handler signature
SBH007 | Shiny.BluetoothLE.Hosting | Error | Invalid handler declaration
SBH008 | Shiny.BluetoothLE.Hosting | Error | Invalid L2CAP PSM publication
SBH009 | Shiny.BluetoothLE.Hosting | Error | L2CAP service needs exactly one channel handler
SBH010 | Shiny.BluetoothLE.Hosting | Error | Characteristic declared twice in one merged service
SBH011 | Shiny.BluetoothLE.Hosting | Warning | Merged services disagree on Primary
SBH012 | Shiny.BluetoothLE.Hosting | Warning | Option combination is not expressible
SBH013 | Shiny.BluetoothLE.Hosting | Error | ManualRespond requires a WriteRequest parameter and no status return
SBH014 | Shiny.BluetoothLE.Hosting | Error | Class level NotifyCharacteristic needs a Name
