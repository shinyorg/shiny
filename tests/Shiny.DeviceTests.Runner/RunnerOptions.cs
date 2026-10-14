using System.Globalization;

namespace Shiny.DeviceTests.Runner;


public sealed class RunnerOptions
{
    public string Platform { get; private set; } = null!;
    public string? Device { get; private set; }
    public string? Agent { get; private set; }
    public string? PeerPlatform { get; private set; }
    public string? PeerDevice { get; private set; }
    public string? PeerAgent { get; private set; }
    public string? Filter { get; private set; }
    public string? HostUrl { get; private set; }
    public string? JUnitPath { get; private set; }
    public int Port { get; private set; } = DeviceTestConstants.RunnerPort;
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromMinutes(30);
    public bool Launch { get; private set; }
    public bool Grant { get; private set; } = true;
    public bool DismissAlerts { get; private set; } = true;
    public bool List { get; private set; }
    public bool ServeOnly { get; private set; }
    public bool Verbose { get; private set; }


    public static RunnerOptions? Parse(string[] args)
    {
        var o = new RunnerOptions();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");

            switch (args[i])
            {
                case "--platform": o.Platform = Next().ToLowerInvariant(); break;
                case "--device": o.Device = Next(); break;
                case "--agent": o.Agent = Next(); break;
                case "--peer": o.PeerPlatform = Next().ToLowerInvariant(); break;
                case "--peer-device": o.PeerDevice = Next(); break;
                case "--peer-agent": o.PeerAgent = Next(); break;
                case "--filter": o.Filter = Next(); break;
                case "--host-url": o.HostUrl = Next(); break;
                case "--junit": o.JUnitPath = Next(); break;
                case "--port": o.Port = Int32.Parse(Next(), CultureInfo.InvariantCulture); break;
                case "--timeout": o.Timeout = TimeSpan.FromMinutes(Double.Parse(Next(), CultureInfo.InvariantCulture)); break;
                case "--launch": o.Launch = true; break;
                case "--no-grant": o.Grant = false; break;
                case "--no-alerts": o.DismissAlerts = false; break;
                case "--list": o.List = true; break;
                case "--serve": o.ServeOnly = true; break;
                case "--verbose" or "-v": o.Verbose = true; break;
                case "--help" or "-h": return null;
                default: throw new ArgumentException($"Unknown argument {args[i]}");
            }
        }
        return o.Platform == null ? null : o;
    }


    public static void PrintUsage() => Console.WriteLine("""
        Shiny device test runner - drives Shiny.DeviceTests through its DevFlow agent and hosts the fixtures.
        Build, install and start the app first (see tests/Shiny.DeviceTests/readme.md).

        dotnet run --project tests/Shiny.DeviceTests.Runner -- --platform <ios|android|maccatalyst> [options]

          --device <udid|serial>      default: booted iOS simulator / first adb device
          --agent <url>               DevFlow agent URL (required for a physical iPhone: http://<ip>:9301)
          --peer <platform>           second copy of the app for two-device tests (the BLE loop)
          --peer-device <id>          --peer-agent <url>
          --filter <Suite[.Test],..>  prefix match, comma separated
          --launch                    launch the installed app(s) before running
          --no-grant                  skip simctl/adb permission pre-grants
          --no-alerts                 don't auto-accept system dialogs
          --host-url <url>            how the app reaches this runner (default: worked out per target)
          --port <n>                  runner port (default 5199)
          --junit <path>              write JUnit XML results
          --timeout <minutes>         whole run (default 30)
          --list                      list the tests and exit
          --serve                     only host the fixtures (run tests from the app's button)
          -v, --verbose               print every test's log
        """);
}
