using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Sample.Shared.Maui.Ai;

/// <summary>
/// Turns a GitHub Copilot subscription into an <see cref="IChatClient"/>.
///
/// The Copilot API speaks OpenAI, so once we have a session token this is just
/// <c>OpenAIClient</c> + <c>AsIChatClient()</c>. The interesting part is getting the token on a
/// phone with no redirect URI: GitHub's <b>device code flow</b>. We show the user a short code,
/// copy it to their clipboard, open the browser, and poll until they've authorised.
///
/// Two tokens are in play:
///   * the GitHub OAuth token — long-lived, stored in <see cref="SecureStorage"/>
///   * the Copilot session token — short-lived, exchanged on demand, never persisted
///
/// Borrowed from the Shiny.Music sample so the AI Assistant page can drive the Shiny
/// <c>*.Extensions.AI</c> tool surfaces against a Copilot chat client.
/// </summary>
public class GitHubCopilotChatClientProvider
{
    // the well-known VS Code / Copilot Chat client id used by the editor integrations
    const string ClientId = "Iv1.b507a08c87ecfe98";
    const string Scope = "read:user";
    const string DeviceCodeUrl = "https://github.com/login/device/code";
    const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
    const string CopilotTokenUrl = "https://api.github.com/copilot_internal/v2/token";
    const string CopilotBaseUrl = "https://api.githubcopilot.com";
    const string DefaultModel = "gpt-4o";
    const string TokenStorageKey = "github_oauth_token";

    static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "Accept", "application/json" },
            { "User-Agent", "ShinySample/1.0" }
        }
    };

    readonly SemaphoreSlim authGate = new(1, 1);
    string? cachedCopilotToken;
    DateTimeOffset copilotTokenExpiry = DateTimeOffset.MinValue;

    /// <summary>Raised when the stored GitHub token changes. Non-null means signed in.</summary>
    public event Action<string?>? AccessTokenChanged;

    /// <summary>
    /// Raised when a device code has been issued and we're waiting for the user to authorise in the browser.
    /// Arguments: the user code to enter, and the verification URL. Lets the UI show the code persistently
    /// (the modal alert is easy to miss) so a long poll doesn't look like a hang.
    /// </summary>
    public event Action<string, string>? DeviceCodeReady;

    public bool IsAuthenticated
        => SecureStorage.Default.GetAsync(TokenStorageKey).GetAwaiter().GetResult() is not null;


    /// <summary>
    /// Builds an <see cref="IChatClient"/> against the Copilot endpoint, running the device-code
    /// flow first if we aren't signed in yet.
    /// </summary>
    public async Task<IChatClient> GetChatClient(CancellationToken cancelToken = default)
    {
        if (await SecureStorage.Default.GetAsync(TokenStorageKey).ConfigureAwait(false) is null)
            await this.Authenticate(cancelToken).ConfigureAwait(false);

        string copilotToken;
        try
        {
            copilotToken = await this.GetCopilotToken(cancelToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // the stored GitHub token was revoked or expired - start over
            this.SignOut();
            await this.Authenticate(cancelToken).ConfigureAwait(false);
            copilotToken = await this.GetCopilotToken(cancelToken).ConfigureAwait(false);
        }

        var options = new OpenAIClientOptions { Endpoint = new Uri(CopilotBaseUrl) };
        options.AddPolicy(new CopilotHeadersPolicy(), PipelinePosition.PerCall);

        return new OpenAIClient(new ApiKeyCredential(copilotToken), options)
            .GetChatClient(DefaultModel)
            .AsIChatClient();
    }


    /// <summary>
    /// Runs the GitHub device-code flow: show the code, copy it, open the browser, poll until the
    /// user authorises. Safe to call concurrently — only one flow runs at a time.
    /// </summary>
    public async Task Authenticate(CancellationToken cancelToken = default)
    {
        await this.authGate.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            if (await SecureStorage.Default.GetAsync(TokenStorageKey).ConfigureAwait(false) is not null)
                return; // someone else won the race

            var device = await this.StartDeviceFlow(cancelToken).ConfigureAwait(false);
            this.DeviceCodeReady?.Invoke(device.UserCode, device.VerificationUri);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page
                    ?? throw new InvalidOperationException("No active page to show the sign-in dialog on.");

                await Clipboard.Default.SetTextAsync(device.UserCode);
                await page.DisplayAlertAsync(
                    "GitHub Sign In",
                    $"Your device code is:\n\n{device.UserCode}\n\nIt's on your clipboard. Press OK to open GitHub and paste it.",
                    "OK"
                );
                await Browser.Default.OpenAsync(device.VerificationUri, BrowserLaunchMode.SystemPreferred);
            }).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            cts.CancelAfter(TimeSpan.FromSeconds(device.ExpiresIn));

            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(device.Interval), cts.Token).ConfigureAwait(false);

                if (await this.PollForToken(device, cts.Token).ConfigureAwait(false))
                {
                    this.AccessTokenChanged?.Invoke(
                        await SecureStorage.Default.GetAsync(TokenStorageKey).ConfigureAwait(false)
                    );
                    return;
                }
            }
            cts.Token.ThrowIfCancellationRequested();
        }
        finally
        {
            this.authGate.Release();
        }
    }


    public void SignOut()
    {
        SecureStorage.Default.Remove(TokenStorageKey);
        this.cachedCopilotToken = null;
        this.copilotTokenExpiry = DateTimeOffset.MinValue;
        this.AccessTokenChanged?.Invoke(null);
    }


    async Task<DeviceCodeResponse> StartDeviceFlow(CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["scope"] = Scope
        });

        using var response = await Http.PostAsync(DeviceCodeUrl, content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceCodeResponse>(ct).ConfigureAwait(false))!;
    }


    async Task<bool> PollForToken(DeviceCodeResponse device, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["device_code"] = device.DeviceCode,
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
        });

        HttpResponseMessage response;
        try
        {
            response = await Http.PostAsync(AccessTokenUrl, content, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return false;   // transient network blip - keep polling until the expiry fires
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(ct).ConfigureAwait(false);

            if (!String.IsNullOrEmpty(result?.AccessToken))
            {
                await SecureStorage.Default.SetAsync(TokenStorageKey, result.AccessToken).ConfigureAwait(false);
                return true;
            }
        }
        return false;   // authorization_pending
    }


    async Task<string> GetCopilotToken(CancellationToken ct)
    {
        if (this.cachedCopilotToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < this.copilotTokenExpiry)
            return this.cachedCopilotToken;

        var githubToken = await SecureStorage.Default.GetAsync(TokenStorageKey).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Not signed in to GitHub.");

        using var request = new HttpRequestMessage(HttpMethod.Get, CopilotTokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("token", githubToken);

        using var response = await Http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = (await response.Content.ReadFromJsonAsync<CopilotTokenResponse>(ct).ConfigureAwait(false))!;
        this.cachedCopilotToken = result.Token;
        this.copilotTokenExpiry = DateTimeOffset.FromUnixTimeSeconds(result.ExpiresAt);

        return this.cachedCopilotToken;
    }
}


record DeviceCodeResponse(
    [property: JsonPropertyName("device_code")] string DeviceCode,
    [property: JsonPropertyName("user_code")] string UserCode,
    [property: JsonPropertyName("verification_uri")] string VerificationUri,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("interval")] int Interval
);

record AccessTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription
);

record CopilotTokenResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("expires_at")] long ExpiresAt
);


/// <summary>Copilot rejects requests that don't look like they came from an editor integration.</summary>
file class CopilotHeadersPolicy : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddHeaders(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddHeaders(message);
        return ProcessNextAsync(message, pipeline, currentIndex);
    }

    static void AddHeaders(PipelineMessage message)
    {
        message.Request.Headers.Set("Editor-Version", "vscode/1.96.2");
        message.Request.Headers.Set("Editor-Plugin-Version", "copilot-chat/0.26.7");
        message.Request.Headers.Set("Copilot-Integration-Id", "vscode-chat");
        message.Request.Headers.Set("Openai-Intent", "conversation-panel");
    }
}
