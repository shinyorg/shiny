using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace Shiny.DeviceTests.Runner;


/// <summary>
/// One running copy of Shiny.DeviceTests - the device under test or its BLE peer - and the host-side controls
/// for it (simctl / adb / devicectl).
/// </summary>
public sealed class TargetDevice
{
    public const string AppId = "org.shiny.devicetests";

    public required string Platform { get; init; }
    public string? Id { get; init; }
    public required bool IsSimulator { get; init; }
    public required string AgentUrl { get; init; }

    public bool IsIos => this.Platform == "ios";
    public bool IsAndroid => this.Platform == "android";
    public bool IsMacCatalyst => this.Platform == "maccatalyst";

    public bool CanSetLocation => this.IsSimulator && (this.IsIos || this.IsAndroid);
    public bool CanPush => this.IsSimulator && this.IsIos;
    public bool CanPlaceCall => this.IsSimulator && this.IsAndroid;

    public override string ToString() => $"{this.Platform}{(this.IsSimulator ? " simulator" : "")} {this.Id} (agent {this.AgentUrl})";


    public static async Task<TargetDevice> ResolveAsync(string platform, string? id, string? agentUrl, CancellationToken ct)
    {
        switch (platform)
        {
            case "ios":
                var simulators = await GetBootedSimulatorsAsync(ct);
                id ??= simulators.FirstOrDefault() ?? throw new InvalidOperationException("No booted iOS simulator - boot one or pass --device <udid>");
                var isSim = simulators.Contains(id);
                if (!isSim && agentUrl == null)
                    throw new InvalidOperationException("A physical iPhone needs --agent http://<phone-ip>:" + DeviceTestConstants.IosAgentPort);

                return new TargetDevice
                {
                    Platform = platform,
                    Id = id,
                    IsSimulator = isSim,
                    AgentUrl = agentUrl ?? $"http://127.0.0.1:{DeviceTestConstants.IosAgentPort}"
                };

            case "android":
                id ??= (await GetAdbDevicesAsync(ct)).FirstOrDefault() ?? throw new InvalidOperationException("No Android device/emulator attached (adb devices)");
                if (agentUrl == null)
                {
                    // the agent listens on the device - forward it to the same port here
                    var port = DeviceTestConstants.AndroidAgentPort.ToString(CultureInfo.InvariantCulture);
                    await Shell.RunCheckedAsync(Shell.Adb, ["-s", id, "forward", $"tcp:{port}", $"tcp:{port}"], ct);
                }
                return new TargetDevice
                {
                    Platform = platform,
                    Id = id,
                    IsSimulator = id.StartsWith("emulator-", StringComparison.Ordinal),
                    AgentUrl = agentUrl ?? $"http://127.0.0.1:{DeviceTestConstants.AndroidAgentPort}"
                };

            case "maccatalyst":
                return new TargetDevice
                {
                    Platform = platform,
                    IsSimulator = false,
                    AgentUrl = agentUrl ?? $"http://127.0.0.1:{DeviceTestConstants.MacCatalystAgentPort}"
                };

            default:
                throw new ArgumentException($"Unknown platform '{platform}' - use ios, android or maccatalyst");
        }
    }


    // the URL the app uses to reach this runner
    public string GetRunnerUrl(int port)
    {
        if (this.IsAndroid && this.IsSimulator)
            return $"http://10.0.2.2:{port}";

        if (this.IsSimulator || this.IsMacCatalyst)
            return $"http://127.0.0.1:{port}";

        return $"http://{GetLanAddress()}:{port}";
    }


    public async Task SetLocationAsync(double latitude, double longitude, CancellationToken ct)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lng = longitude.ToString(CultureInfo.InvariantCulture);

        if (this.IsIos && this.IsSimulator)
            await Shell.RunCheckedAsync("xcrun", ["simctl", "location", this.Id!, "set", $"{lat},{lng}"], ct);
        else if (this.IsAndroid && this.IsSimulator)
            await Shell.RunCheckedAsync(Shell.Adb, ["-s", this.Id!, "emu", "geo", "fix", lng, lat], ct);  // longitude first
        else
            throw new NotSupportedException($"Cannot simulate location on {this}");
    }


    public async Task SendPushAsync(string payloadJson, CancellationToken ct)
    {
        if (!this.CanPush)
            throw new NotSupportedException($"Cannot deliver a simulated push to {this}");

        var file = Path.Combine(Path.GetTempPath(), $"shiny-devicetests-push-{Guid.NewGuid():N}.apns");
        await File.WriteAllTextAsync(file, payloadJson, ct);
        try
        {
            await Shell.RunCheckedAsync("xcrun", ["simctl", "push", this.Id!, AppId, file], ct);
        }
        finally
        {
            File.Delete(file);
        }
    }


    // pre-grants what the suites request so no system dialog is needed (the alert watcher covers the rest).
    // Returns true when granting killed the app - simctl privacy terminates it - so it must be relaunched.
    public async Task<bool> GrantPermissionsAsync(CancellationToken ct)
    {
        if (this.IsIos && this.IsSimulator)
        {
            foreach (var service in new[] { "location-always", "calendar", "contacts" })
            {
                var result = await Shell.RunAsync("xcrun", ["simctl", "privacy", this.Id!, "grant", service, AppId], ct);
                if (!result.Success)
                    Console.WriteLine($"  ! simctl privacy grant {service}: {result}");
            }
            return true;
        }
        else if (this.IsAndroid)
        {
            string[] permissions =
            [
                "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.ACCESS_COARSE_LOCATION",
                "android.permission.ACCESS_BACKGROUND_LOCATION",
                "android.permission.POST_NOTIFICATIONS",
                "android.permission.BLUETOOTH_SCAN",
                "android.permission.BLUETOOTH_CONNECT",
                "android.permission.BLUETOOTH_ADVERTISE",
                "android.permission.READ_CONTACTS",
                "android.permission.WRITE_CONTACTS",
                "android.permission.READ_CALENDAR",
                "android.permission.WRITE_CALENDAR",
                "android.permission.NEARBY_WIFI_DEVICES",
                // Android 17+: reaching the runner at 10.0.2.2 / the LAN and multicast discovery are runtime-gated
                "android.permission.ACCESS_LOCAL_NETWORK"
            ];
            // permissions that don't exist on this API level just fail - that is fine
            foreach (var permission in permissions)
                await Shell.RunAsync(Shell.Adb, ["-s", this.Id!, "shell", "pm", "grant", AppId, permission], ct);

            await Shell.RunAsync(Shell.Adb, ["-s", this.Id!, "shell", "appops", "set", AppId, "SCHEDULE_EXACT_ALARM", "allow"], ct);

            // Shiny.Mobile.CallDirectory screens calls only while the app holds this role - no dialog to tap
            await Shell.RunAsync(Shell.Adb, ["-s", this.Id!, "shell", "cmd", "role", "add-role-holder", "android.app.role.CALL_SCREENING", AppId], ct);
        }
        return false;
    }


    public async Task LaunchAsync(CancellationToken ct)
    {
        if (this.IsIos && this.IsSimulator)
            await Shell.RunCheckedAsync("xcrun", ["simctl", "launch", this.Id!, AppId], ct);
        else if (this.IsIos)
            await Shell.RunCheckedAsync("xcrun", ["devicectl", "device", "process", "launch", "--device", this.Id!, AppId], ct);
        else if (this.IsAndroid)
            await Shell.RunCheckedAsync(Shell.Adb, ["-s", this.Id!, "shell", "monkey", "-p", AppId, "-c", "android.intent.category.LAUNCHER", "1"], ct);
        else
            await Shell.RunCheckedAsync("open", ["-b", AppId], ct);
    }


    // rings the emulator from a number, hangs up, and returns the CallLog.Calls.TYPE Android recorded for it
    public async Task<int?> PlaceCallAsync(string number, int ringSeconds, CancellationToken ct)
    {
        if (!this.CanPlaceCall)
            throw new NotSupportedException($"Cannot place a call to {this}");

        var started = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Shell.RunCheckedAsync(Shell.Adb, ["-s", this.Id!, "emu", "gsm", "call", number], ct);
        await Task.Delay(TimeSpan.FromSeconds(ringSeconds), ct);
        await Shell.RunAsync(Shell.Adb, ["-s", this.Id!, "emu", "gsm", "cancel", number], ct);

        // the call log row lands a moment after the call ends
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(1000, ct);
            var output = await Shell.RunCheckedAsync(Shell.Adb, [
                "-s", this.Id!, "shell", "content", "query",
                "--uri", "content://call_log/calls",
                "--projection", "type:date",
                "--where", $"\"number LIKE '%{number}' AND date >= {started}\"",
                "--sort", "\"date DESC\""
            ], ct);

            // "Row: 0 type=6, date=..."
            var marker = output.IndexOf("type=", StringComparison.Ordinal);
            if (marker >= 0)
            {
                var digits = new string(output[(marker + 5)..].TakeWhile(char.IsAsciiDigit).ToArray());
                return Int32.Parse(digits, CultureInfo.InvariantCulture);
            }
        }
        return null;
    }


    // taps the accept-style button on any system dialog (permission prompts the pre-grant could not cover)
    public async Task DismissAlertAsync(CancellationToken ct)
    {
        List<string> args = ["devflow", "ui", "alert", "dismiss", "-p", this.Platform];
        if (this.Id != null)
            args.AddRange(["--udid", this.Id]);

        await Shell.RunAsync("maui", args, ct);
    }


    static async Task<List<string>> GetBootedSimulatorsAsync(CancellationToken ct)
    {
        var json = await Shell.RunCheckedAsync("xcrun", ["simctl", "list", "devices", "booted", "-j"], ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("devices")
            .EnumerateObject()
            .SelectMany(x => x.Value.EnumerateArray())
            .Where(x => x.GetProperty("state").GetString() == "Booted")
            .Select(x => x.GetProperty("udid").GetString()!)
            .ToList();
    }


    static async Task<List<string>> GetAdbDevicesAsync(CancellationToken ct)
    {
        var output = await Shell.RunCheckedAsync(Shell.Adb, ["devices"], ct);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .Select(x => x.Split('\t'))
            .Where(x => x.Length == 2 && x[1] == "device")
            .Select(x => x[0])
            .ToList();
    }


    static string GetLanAddress() => NetworkInterface
        .GetAllNetworkInterfaces()
        .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(x => x.GetIPProperties().UnicastAddresses)
        .Select(x => x.Address)
        .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork && !x.ToString().StartsWith("169.254", StringComparison.Ordinal))
        ?.ToString() ?? throw new InvalidOperationException("No LAN address - pass --host-url");
}
