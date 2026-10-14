using System.Net.Http.Json;

namespace Shiny.DeviceTests.Framework;


/// <summary>
/// Talks back to Shiny.DeviceTests.Runner on the host machine: environment control (simulated location,
/// pushes), the peer device, and the HTTP fixtures the network modules run against.
/// </summary>
public sealed class HostClient
{
    readonly HttpClient http;


    public HostClient(string? baseUrl)
    {
        this.BaseUrl = baseUrl?.TrimEnd('/');
        this.http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        if (this.BaseUrl != null)
            this.http.BaseAddress = new Uri(this.BaseUrl + "/");
    }


    public string? BaseUrl { get; }

    // absolute URL to a runner endpoint, e.g. Url("files/download/1024")
    public string Url(string relative)
        => (this.BaseUrl ?? throw new SkipException("No runner attached")) + "/" + relative.TrimStart('/');


    public async Task<HostInfo?> TryGetInfoAsync(string platform, bool isSimulator)
    {
        if (this.BaseUrl == null)
            return null;

        return await this.http
            .GetFromJsonAsync($"env/info?platform={platform}&simulator={isSimulator}", DeviceTestJsonContext.Default.HostInfo)
            .ConfigureAwait(false);
    }


    // moves the simulated location (iOS simulator: simctl, Android emulator: adb emu geo fix)
    public async Task SetLocationAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var response = await this.http
            .PostAsJsonAsync("env/location", new SetLocationRequest(latitude, longitude), DeviceTestJsonContext.Default.SetLocationRequest, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccess(response).ConfigureAwait(false);
    }


    // delivers an APNs payload to this simulator (xcrun simctl push)
    public async Task SendPushAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var response = await this.http
            .PostAsJsonAsync("env/push", new SendPushRequest(payloadJson), DeviceTestJsonContext.Default.SendPushRequest, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccess(response).ConfigureAwait(false);
    }


    // rings this device from a number (Android emulator) and returns the call log type Android recorded
    public async Task<PlaceCallResult> PlaceCallAsync(string number, int ringSeconds, CancellationToken cancellationToken = default)
    {
        var response = await this.http
            .PostAsJsonAsync("env/call", new PlaceCallRequest(number, ringSeconds), DeviceTestJsonContext.Default.PlaceCallRequest, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccess(response).ConfigureAwait(false);

        return (await response.Content
            .ReadFromJsonAsync(DeviceTestJsonContext.Default.PlaceCallResult, cancellationToken)
            .ConfigureAwait(false))!;
    }


    // runs an IPeerCommand on the other device (relayed by the runner through that device's DevFlow agent)
    public async Task<PeerCommandResponse> PeerAsync(string command, Dictionary<string, string>? args = null, CancellationToken cancellationToken = default)
    {
        var response = await this.http
            .PostAsJsonAsync("env/peer", new PeerCommandRequest(command, args), DeviceTestJsonContext.Default.PeerCommandRequest, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccess(response).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync(DeviceTestJsonContext.Default.PeerCommandResponse, cancellationToken)
            .ConfigureAwait(false);

        if (result is not { Success: true })
            throw new InvalidOperationException($"Peer command '{command}' failed: {result?.Error}");

        return result;
    }


    public Task<HttpResponseMessage> GetAsync(string relative, CancellationToken cancellationToken = default)
        => this.http.GetAsync(relative.TrimStart('/'), cancellationToken);

    public Task<HttpResponseMessage> PostAsync(string relative, HttpContent? content, CancellationToken cancellationToken = default)
        => this.http.PostAsync(relative.TrimStart('/'), content, cancellationToken);

    public Task<HttpResponseMessage> DeleteAsync(string relative, CancellationToken cancellationToken = default)
        => this.http.DeleteAsync(relative.TrimStart('/'), cancellationToken);


    static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new InvalidOperationException($"Runner returned {(int)response.StatusCode}: {body}");
    }
}
