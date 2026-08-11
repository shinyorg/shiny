using Microsoft.CodeAnalysis;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


static class Diagnostics
{
    const string Category = "Shiny.BluetoothLE.Hosting";


    public static readonly DiagnosticDescriptor NotPartial = Error(
        "SBH001",
        "BLE hosted type must be a top level non-generic partial class",
        "'{0}' must be declared 'partial' and must not be static, generic, or a nested type"
    );

    public static readonly DiagnosticDescriptor InvalidUuid = Error(
        "SBH002",
        "Invalid Bluetooth UUID",
        "'{0}' is not a valid Bluetooth UUID - use the 16 bit ('180D'), 32 bit ('0000180D') or full 128 bit ('0000180D-0000-1000-8000-00805F9B34FB') form"
    );

    public static readonly DiagnosticDescriptor MissingBleService = Error(
        "SBH003",
        "Characteristic handler outside a BLE service",
        "'{0}' declares a characteristic attribute but its containing type '{1}' has no [BleService]"
    );

    public static readonly DiagnosticDescriptor DuplicateHandler = Error(
        "SBH004",
        "Duplicate characteristic handler",
        "Characteristic '{0}' already has a {1} handler on '{2}' - a characteristic may only declare one handler of each kind"
    );

    public static readonly DiagnosticDescriptor RequestResponseConflict = Error(
        "SBH005",
        "Request/response characteristic conflicts with a write or notify handler",
        "Characteristic '{0}' is declared as request/response, which already registers write and notify - remove the separate {1} declaration"
    );

    public static readonly DiagnosticDescriptor UnsupportedSignature = Error(
        "SBH006",
        "Unsupported handler signature",
        "'{0}' cannot be bound as a {1} handler - {2}"
    );

    public static readonly DiagnosticDescriptor InvalidHandlerModifier = Error(
        "SBH007",
        "Invalid handler declaration",
        "'{0}' must be an instance method that is not static, abstract or generic, and must not use ref, out or in parameters"
    );

    public static readonly DiagnosticDescriptor InvalidPsmPublication = Error(
        "SBH008",
        "Invalid L2CAP PSM publication",
        "{0}"
    );

    public static readonly DiagnosticDescriptor ChannelHandlerCount = Error(
        "SBH009",
        "L2CAP service needs exactly one channel handler",
        "'{0}' must declare exactly one [OnChannelOpened] method, but declares {1}"
    );

    public static readonly DiagnosticDescriptor MergedCharacteristicConflict = Error(
        "SBH010",
        "Characteristic declared twice in one merged service",
        "Characteristic '{0}' is declared by both '{1}' and '{2}', which merge into service '{3}'"
    );

    public static readonly DiagnosticDescriptor MergedPrimaryMismatch = Warning(
        "SBH011",
        "Merged services disagree on Primary",
        "'{0}' sets Primary={1} but service '{2}' was first declared with Primary={3} - the first declaration wins"
    );

    public static readonly DiagnosticDescriptor UnexpressibleOptions = Warning(
        "SBH012",
        "Option combination is not expressible",
        "{0} on '{1}' cannot be expressed - Shiny's {2} enum declares its members without explicit flag values, so only one may be selected; using {3}"
    );

    public static readonly DiagnosticDescriptor ManualRespondMisuse = Error(
        "SBH013",
        "ManualRespond requires a WriteRequest parameter and no status return",
        "'{0}' sets ManualRespond, so it must take a WriteRequest parameter and must not return GattState"
    );

    public static readonly DiagnosticDescriptor NotifyNameRequired = Error(
        "SBH014",
        "Class level [NotifyCharacteristic] needs a Name",
        "[NotifyCharacteristic(\"{0}\")] on '{1}' must set Name - there is no method name to derive the generated Notify members from"
    );


    static DiagnosticDescriptor Error(string id, string title, string format)
        => new(id, title, format, Category, DiagnosticSeverity.Error, true);

    static DiagnosticDescriptor Warning(string id, string title, string format)
        => new(id, title, format, Category, DiagnosticSeverity.Warning, true);
}
