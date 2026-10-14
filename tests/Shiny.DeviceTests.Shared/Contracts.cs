using System.Text.Json.Serialization;

namespace Shiny.DeviceTests;

// Wire contracts shared by the device test app and the host runner (linked into both projects).
// The app exposes them through its DevFlow extension; the runner serves the environment endpoints.

public static class DeviceTestConstants
{
    // DevFlow extension namespace - routes live under /api/v1/ext/org.shiny.devicetests
    public const string ExtensionNamespace = "org.shiny.devicetests";
    public const string ExtensionRoot = "/api/v1/ext/" + ExtensionNamespace;

    // each head gets its own agent port so the iOS simulator and Mac Catalyst (which share the
    // Mac's network stack) can run side by side as the two halves of the BLE loop
    public const int IosAgentPort = 9301;
    public const int MacCatalystAgentPort = 9302;
    public const int AndroidAgentPort = 9303;

    public const int RunnerPort = 5199;
    public const int PrinterPort = 9100;
}

public enum TestOutcome
{
    Passed,
    Failed,
    Skipped
}

// what the runner can do to the environment the app is running in - tests skip when theirs is missing
[Flags]
public enum HostCapability
{
    None = 0,
    // the runner's HTTP fixtures (transfers, data sync, printer capture) are reachable
    Server = 1,
    Location = 2,
    Push = 4,
    // a second device running this app is attached (the other half of the BLE loop)
    Peer = 8,
    // the runner can ring this device (Android emulator: adb emu gsm call) and read its call log
    PhoneCall = 16
}

public record TestInfo(string Suite, string Name);

public record TestRunRequest(string? Filter, string? HostUrl);

public record TestRunStarted(string RunId, int Total);

public record TestResult(
    string Suite,
    string Name,
    TestOutcome Outcome,
    double DurationMs,
    string? Message,
    string? StackTrace,
    string[] Log
);

public record TestRunStatus(
    string? RunId,
    bool Running,
    int Total,
    int Passed,
    int Failed,
    int Skipped,
    string? Current,
    List<TestResult> Results
);

public record PeerCommandRequest(string Command, Dictionary<string, string>? Args);

public record PeerCommandResponse(bool Success, string? Error, Dictionary<string, string>? Values);

public record HostInfo(string Platform, bool IsSimulator, HostCapability Capabilities, string? PeerPlatform);

public record SetLocationRequest(double Latitude, double Longitude);

public record SendPushRequest(string PayloadJson);

public record PrinterCapture(int Connections, string Base64Data);

// rings the device from Number for RingSeconds, hangs up, and reports the call log entry Android wrote
public record PlaceCallRequest(string Number, int RingSeconds);

// CallLog.Calls.TYPE: 1 incoming, 3 missed, 5 rejected, 6 blocked; null when no entry was written
public record PlaceCallResult(int? CallLogType);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(TestInfo))]
[JsonSerializable(typeof(List<TestInfo>))]
[JsonSerializable(typeof(TestRunRequest))]
[JsonSerializable(typeof(TestRunStarted))]
[JsonSerializable(typeof(TestResult))]
[JsonSerializable(typeof(TestRunStatus))]
[JsonSerializable(typeof(PeerCommandRequest))]
[JsonSerializable(typeof(PeerCommandResponse))]
[JsonSerializable(typeof(HostInfo))]
[JsonSerializable(typeof(SetLocationRequest))]
[JsonSerializable(typeof(SendPushRequest))]
[JsonSerializable(typeof(PrinterCapture))]
[JsonSerializable(typeof(PlaceCallRequest))]
[JsonSerializable(typeof(PlaceCallResult))]
public partial class DeviceTestJsonContext : JsonSerializerContext;
