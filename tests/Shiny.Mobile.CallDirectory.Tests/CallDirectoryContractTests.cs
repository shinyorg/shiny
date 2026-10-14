using System.Diagnostics;
using System.Text.Json;
using Shiny.CallDirectory.Infrastructure;

namespace Shiny.CallDirectory.Tests;


/// <summary>
/// Builds the fake CallKit once: the extension's real loader (ShinyCallDirectoryLoader.swift) plus a stand-in for iOS
/// that enforces CallKit's load rules.
/// </summary>
public sealed class FakeCallKitFixture
{
    public string? Executable { get; }

    public FakeCallKitFixture()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var src = Path.Combine(AppContext.BaseDirectory, "FakeCallKit");
        this.Executable = Path.Combine(Directory.CreateTempSubdirectory("fakecallkit").FullName, "fakecallkit");
        var result = CallDirectoryContractTests.Run(
            "xcrun",
            $"swiftc -O -module-name FakeCallKit \"{Path.Combine(src, "ShinyCallDirectoryLoader.swift")}\" \"{Path.Combine(src, "main.swift")}\" -o \"{this.Executable}\""
        );
        if (result.ExitCode != 0)
            throw new InvalidOperationException("swiftc failed: " + result.Output);
    }
}


/// <summary>
/// What the C# store writes, run through what the Swift extension loads, must leave "iOS" holding exactly the stored
/// list - after a first load, after incremental loads, after ReplaceAll, after iOS forcing a full load.
/// </summary>
public class CallDirectoryContractTests(FakeCallKitFixture fakeCallKit) : IClassFixture<FakeCallKitFixture>, IDisposable
{
    readonly string dir = Directory.CreateTempSubdirectory("shinycd-contract").FullName;
    CallDirectoryStore? store;

    CallDirectoryStore Store => this.store ??= new(Path.Combine(this.dir, "store.db"));
    string StatePath => Path.Combine(this.dir, "ios-state.json");
    public void Dispose() => Directory.Delete(this.dir, true);

    // the oracle: what the list should be, kept independently of the store
    readonly Dictionary<long, CallDirectoryEntry> expected = [];


    [MacFact]
    public async Task FirstLoadIsFull()
    {
        await this.Set(new CallDirectoryEntry(18005550100, "Support"), new CallDirectoryEntry(18005550101, null, CallDirectoryEntryType.Block), new CallDirectoryEntry(18005550102, "Sales", CallDirectoryEntryType.Allow));
        Assert.Equal("full", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task EmptyStoreLoads()
    {
        Assert.Equal("full", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task IncrementalHandlesEveryTransition()
    {
        await this.Set(
            new CallDirectoryEntry(10, "rename me"),
            new CallDirectoryEntry(11, null, CallDirectoryEntryType.Block),
            new CallDirectoryEntry(12, "become blocked"),
            new CallDirectoryEntry(13, null, CallDirectoryEntryType.Block),
            new CallDirectoryEntry(14, "allow then identify", CallDirectoryEntryType.Allow),
            new CallDirectoryEntry(15, "remove me")
        );
        await this.Reload();

        await this.Set(
            new CallDirectoryEntry(10, "renamed"),                                  // label change: remove + add
            new CallDirectoryEntry(11, "unblocked"),                                // block -> identify
            new CallDirectoryEntry(12, null, CallDirectoryEntryType.Block),         // identify -> block
            new CallDirectoryEntry(14, "allow then identify"),                      // allow -> identify, same label
            new CallDirectoryEntry(16, "brand new"),
            new CallDirectoryEntry(9, null, CallDirectoryEntryType.Block)           // below everything already loaded
        );
        await this.Remove(15, 13, 999);                          // 999 was never stored
        await this.Set(new CallDirectoryEntry(17, "added then removed"));
        await this.Remove(17);

        Assert.Equal("incremental", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task SeveralEditsToOneNumberBetweenLoads()
    {
        await this.Set(new CallDirectoryEntry(20, "first"));
        await this.Reload();

        await this.Set(new CallDirectoryEntry(20, null, CallDirectoryEntryType.Block));
        await this.Remove(20);
        await this.Set(new CallDirectoryEntry(20, "third"));
        await this.Set(new CallDirectoryEntry(20, "fourth", CallDirectoryEntryType.Allow));

        Assert.Equal("incremental", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task ReplaceAllAfterIncrementalForcesRemoveAll()
    {
        await this.Set(new CallDirectoryEntry(30, "a"), new CallDirectoryEntry(31, null, CallDirectoryEntryType.Block));
        await this.Reload();
        await this.Set(new CallDirectoryEntry(32, "b"));
        await this.Reload();

        await this.ReplaceAll(new CallDirectoryEntry(31, "now identified"), new CallDirectoryEntry(40, null, CallDirectoryEntryType.Block));
        await this.Set(new CallDirectoryEntry(41, "after replace"));

        Assert.Equal("incremental", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task IosForcedFullLoad()
    {
        await this.Set(new CallDirectoryEntry(50, "a"), new CallDirectoryEntry(51, "b"));
        await this.Reload();
        await this.Set(new CallDirectoryEntry(52, "c"));

        Assert.Equal("full", await this.Reload(full: true));
        this.AssertIosMatches();

        await this.Remove(50);
        Assert.Equal("incremental", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task ReloadWithNothingChanged()
    {
        await this.Set(new CallDirectoryEntry(60, "a"));
        await this.Reload();
        Assert.Equal("incremental", await this.Reload());
        Assert.Equal("incremental", await this.Reload());
        this.AssertIosMatches();
    }


    [MacFact]
    public async Task AppliedChangesAreTrimmed()
    {
        await this.Set(new CallDirectoryEntry(70, "a"), new CallDirectoryEntry(71, "b"));
        await this.Reload();
        await this.Set(new CallDirectoryEntry(72, "c"));
        await this.Reload();

        // the next reload trims what the last one applied
        await this.Reload();
        Assert.Equal(0, await this.Store.PendingChanges(default));
    }


    [MacFact]
    public async Task RandomizedEditsStayInSync()
    {
        var random = new Random(1665);
        var types = Enum.GetValues<CallDirectoryEntryType>();

        for (var round = 0; round < 150; round++)
        {
            var ops = random.Next(1, 8);
            for (var i = 0; i < ops; i++)
            {
                var number = 18005550000 + random.Next(0, 40);
                switch (random.Next(0, 20))
                {
                    case < 11:
                        var label = random.Next(0, 4) == 0 ? null : $"label {random.Next(0, 5)}";
                        await this.Set(new CallDirectoryEntry(number, label, types[random.Next(types.Length)]));
                        break;

                    case < 18:
                        await this.Remove(number);
                        break;

                    case 18:
                        var replacement = Enumerable.Range(0, random.Next(0, 10))
                            .Select(_ => 18005550000 + random.Next(0, 40))
                            .Distinct()
                            .Select(n => new CallDirectoryEntry(n, $"replaced {n % 7}", types[random.Next(types.Length)]))
                            .ToArray();
                        await this.ReplaceAll(replacement);
                        break;

                    default:
                        await this.Clear();
                        break;
                }
            }

            // not every edit batch is followed by a reload, and iOS occasionally asks for a full load
            if (random.Next(0, 3) != 0)
            {
                await this.Reload(full: random.Next(0, 15) == 0);
                this.AssertIosMatches($"round {round}");
            }
        }
    }


    async Task Set(params CallDirectoryEntry[] entries)
    {
        await this.Store.Set(entries, default);
        foreach (var e in entries)
            this.expected[e.PhoneNumber] = e;
    }

    async Task Remove(params long[] numbers)
    {
        await this.Store.Remove(numbers, default);
        foreach (var n in numbers)
            this.expected.Remove(n);
    }

    async Task ReplaceAll(params CallDirectoryEntry[] entries)
    {
        await this.Store.ReplaceAll(entries, default);
        this.expected.Clear();
        foreach (var e in entries)
            this.expected[e.PhoneNumber] = e;
    }

    async Task Clear()
    {
        await this.Store.Clear(default);
        this.expected.Clear();
    }


    /// <summary>What CallDirectoryManager.Reload does: trim what was applied, then have "iOS" load the extension.</summary>
    async Task<string> Reload(bool full = false)
    {
        var state = this.ReadState();
        if (state?.AppliedGeneration is > 0)
            await this.Store.Trim(state.AppliedGeneration.Value, state.AppliedSeq ?? 0, default);

        // a store the app never wrote is a missing file; make sure it exists like it would after any write
        var result = Run(fakeCallKit.Executable!, $"\"{this.Store.Path}\" \"{this.StatePath}\"{(full ? " --full" : "")}");
        Assert.True(result.ExitCode == 0, "CallKit rejected the load: " + result.Output);
        return result.Output.Trim();
    }


    void AssertIosMatches(string? because = null)
    {
        var state = this.ReadState()!;
        var blocked = this.expected.Values.Where(x => x.Type == CallDirectoryEntryType.Block).Select(x => x.PhoneNumber).Order().ToArray();
        var identified = this.expected.Values
            .Where(x => x.Type != CallDirectoryEntryType.Block)
            .ToDictionary(x => x.PhoneNumber.ToString(), x => x.Label ?? "");

        Assert.True(blocked.SequenceEqual(state.Blocked), $"{because} blocked: expected [{string.Join(",", blocked)}] but iOS holds [{string.Join(",", state.Blocked)}]");
        Assert.True(
            identified.Count == state.Identified.Count && identified.All(x => state.Identified.TryGetValue(x.Key, out var l) && l == x.Value),
            $"{because} identified: expected {Describe(identified)} but iOS holds {Describe(state.Identified)}"
        );
    }

    static string Describe(Dictionary<string, string> d) => "{" + string.Join(", ", d.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}")) + "}";


    IosState? ReadState() => File.Exists(this.StatePath)
        ? JsonSerializer.Deserialize<IosState>(File.ReadAllText(this.StatePath), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        : null;

    public record IosState(long[] Blocked, Dictionary<string, string> Identified, long? AppliedGeneration, long? AppliedSeq);


    internal static (int ExitCode, string Output) Run(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo(file, args) { RedirectStandardOutput = true, RedirectStandardError = true })!;
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        return (p.ExitCode, stdout.Result + stderr.Result);
    }
}
