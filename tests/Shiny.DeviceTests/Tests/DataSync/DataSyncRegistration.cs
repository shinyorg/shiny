using Shiny.Data.Sync;

namespace Shiny.DeviceTests.Tests;


public static class DataSyncRegistration
{
    // Endpoint URLs are fixed at registration, before a run hands the app its runner URL, so they point at
    // where the runner is for a simulator/emulator (Mac localhost, or the Android emulator's host alias).
    // On a physical device the suite skips - see DataSyncTests.SetupAsync.
    public static string RunnerBaseUrl { get; } = GetRunnerBaseUrl();


    public static IServiceCollection AddDataSyncDeviceTests(this IServiceCollection services)
    {
        services.AddSingleton<DataSyncTestState>();
        services.AddDataSync<TestDataSyncDelegate>(b => b.RegisterEndpoint<SyncNote>(RunnerBaseUrl + "/sync/notes", ep =>
        {
            ep.TombstoneUrl = RunnerBaseUrl + "/sync/notes/tombstones";
            ep.DefaultConflictPolicy = ConflictPolicy.AskDelegate;
            ep.MaxAttempts = 3;
            ep.RetryBaseDelay = TimeSpan.FromMilliseconds(500);

            // manual pulls only - the SyncJob must not race the suite's PullNow calls
            ep.MinPullInterval = null;
        }));
        services.AddDeviceTestSuite<DataSyncTests>();
        return services;
    }


    static string GetRunnerBaseUrl()
    {
        var port = DeviceTestConstants.RunnerPort;
#if ANDROID
        return $"http://10.0.2.2:{port}";
#else
        return $"http://127.0.0.1:{port}";
#endif
    }
}
