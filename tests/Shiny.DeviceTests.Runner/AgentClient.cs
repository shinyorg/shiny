using System.Net.Http.Json;

namespace Shiny.DeviceTests.Runner;


/// <summary>
/// The runner's view of a device's DevFlow agent: the built-in status route plus the org.shiny.devicetests
/// extension the app registers.
/// </summary>
public sealed class AgentClient(TargetDevice device) : IDisposable
{
    readonly HttpClient http = new()
    {
        BaseAddress = new Uri(device.AgentUrl.TrimEnd('/') + "/"),
        Timeout = TimeSpan.FromMinutes(2)
    };

    public TargetDevice Device => device;


    public async Task WaitForAgentAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        while (true)
        {
            try
            {
                // the extension route only answers once the app (and its DI container) is up
                var response = await this.http.GetAsync(Ext("status"), cts.Token);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!cts.IsCancellationRequested)
            {
            }

            try
            {
                await Task.Delay(1000, cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"DevFlow agent for {device} did not answer within {timeout.TotalSeconds:0}s - is the app running?");
            }
        }
    }


    public async Task<List<TestInfo>> ListAsync(CancellationToken ct)
        => await this.http.GetFromJsonAsync(Ext("tests"), DeviceTestJsonContext.Default.ListTestInfo, ct) ?? [];


    public async Task<TestRunStarted> StartRunAsync(TestRunRequest request, CancellationToken ct)
    {
        var response = await this.http.PostAsync(Ext("run"), Body(request, DeviceTestJsonContext.Default.TestRunRequest), ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Could not start run: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync(ct)}");

        return (await response.Content.ReadFromJsonAsync(DeviceTestJsonContext.Default.TestRunStarted, ct))!;
    }


    public async Task<TestRunStatus> GetStatusAsync(CancellationToken ct)
        => (await this.http.GetFromJsonAsync(Ext("status"), DeviceTestJsonContext.Default.TestRunStatus, ct))!;


    public Task CancelAsync(CancellationToken ct) => this.http.PostAsync(Ext("cancel"), new StringContent(""), ct);


    public async Task<PeerCommandResponse> PeerAsync(PeerCommandRequest request, CancellationToken ct)
    {
        var response = await this.http.PostAsync(Ext("peer"), Body(request, DeviceTestJsonContext.Default.PeerCommandRequest), ct);
        if (!response.IsSuccessStatusCode)
            return new PeerCommandResponse(false, $"Peer agent returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}", null);

        return (await response.Content.ReadFromJsonAsync(DeviceTestJsonContext.Default.PeerCommandResponse, ct))!;
    }


    // the agent's HTTP server needs a Content-Length - JsonContent streams chunked, so buffer the body
    static StringContent Body<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
        => new(System.Text.Json.JsonSerializer.Serialize(value, typeInfo), System.Text.Encoding.UTF8, "application/json");


    static string Ext(string path) => DeviceTestConstants.ExtensionRoot.TrimStart('/') + "/" + path;

    public void Dispose() => this.http.Dispose();
}
