using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Sample.Shared.Maui.Ai;
using Shiny.Calendar.Extensions.AI;
using Shiny.Contacts.Extensions.AI;
using Shiny.Locations.Extensions.AI;
using Shiny.Notifications.Extensions.AI;

namespace Sample.Shared.Maui.Pages.Ai;

/// <summary>
/// A chat screen that drives whichever Shiny <c>*.Extensions.AI</c> tool surfaces are registered on
/// this platform (calendar, contacts, reminders, location) against a GitHub Copilot
/// <see cref="IChatClient"/>. Ask it to add a meeting, find a contact, set a reminder, etc. — the model
/// calls the Shiny tools to do it.
/// </summary>
[ShellMap<AiAssistantPage>("ai")]
public partial class AiAssistantViewModel : ObservableObject
{
    const string SystemPrompt =
        "You are a helpful personal-assistant embedded in a phone app. You have tools to work with the " +
        "user's device calendar (list calendars, search/create/update/delete events), contacts, reminders, " +
        "and current location. Use the tools to fulfil requests such as scheduling a meeting, finding a " +
        "contact's number, or setting a reminder. All device permissions are already granted. When creating " +
        "events use ISO-8601 date-times. Confirm before deleting anything. Keep replies short.";

    readonly GitHubCopilotChatClientProvider copilot;
    readonly IReadOnlyList<AITool> tools;
    readonly List<ChatMessage> history = [];
    IChatClient? client;

    public AiAssistantViewModel(GitHubCopilotChatClientProvider copilot, IServiceProvider services)
    {
        this.copilot = copilot;
        this.tools = CollectTools(services);

        this.history.Add(new ChatMessage(ChatRole.System, SystemPrompt));
        this.IsSignedIn = copilot.IsAuthenticated;
        this.ToolSummary = this.tools.Count == 0
            ? "No AI tools are registered on this platform."
            : $"{this.tools.Count} tools available: {string.Join(", ", this.tools.OfType<AIFunction>().Select(t => t.Name))}";

        copilot.AccessTokenChanged += token =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                this.IsSignedIn = token is not null;
                if (token is not null)
                    Messages.Add(new AiMessage(false, "✅ Signed in to GitHub Copilot. Ask me anything."));
            });

        // The sign-in alert is easy to miss, so also show the device code (and URL) in the chat so a long
        // poll doesn't look like a hang.
        copilot.DeviceCodeReady += (code, url) =>
            MainThread.BeginInvokeOnMainThread(() =>
                Messages.Add(new AiMessage(false,
                    $"To sign in, open {url} and enter code:\n\n{code}\n\n(It's been copied to your clipboard. Waiting for you to authorize…)")));
    }

    // Aggregate whichever tool bundles were registered (each is platform-gated in ShinyRegistrations).
    static IReadOnlyList<AITool> CollectTools(IServiceProvider sp)
    {
        var list = new List<AITool>();
        if (sp.GetService<CalendarAITools>() is { } cal) list.AddRange(cal.Tools);
        if (sp.GetService<ContactAITools>() is { } con) list.AddRange(con.Tools);
        if (sp.GetService<NotificationAITools>() is { } note) list.AddRange(note.Tools);
        if (sp.GetService<LocationAITools>() is { } loc) list.AddRange(loc.Tools);
        return list;
    }

    [ObservableProperty] bool isSignedIn;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string input = "";
    [ObservableProperty] string toolSummary = "";

    public ObservableCollection<AiMessage> Messages { get; } = [];

    [RelayCommand]
    async Task SignIn()
    {
        this.IsBusy = true;
        try
        {
            await this.copilot.GetChatClient();       // triggers the device-code flow
            this.IsSignedIn = this.copilot.IsAuthenticated;
        }
        catch (Exception ex)
        {
            Messages.Add(new AiMessage(false, "⚠ Sign-in failed: " + ex.Message));
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    void SignOut()
    {
        this.copilot.SignOut();
        this.client = null;
        this.IsSignedIn = false;
    }

    [RelayCommand]
    async Task Send()
    {
        var text = this.Input?.Trim();
        if (string.IsNullOrEmpty(text) || this.IsBusy)
            return;

        this.Input = "";
        Messages.Add(new AiMessage(true, text));
        this.IsBusy = true;

        try
        {
            // Build once, wrapping in the function-invocation middleware so the tool loop runs automatically.
            this.client ??= (await this.copilot.GetChatClient())
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            this.IsSignedIn = this.copilot.IsAuthenticated;

            this.history.Add(new ChatMessage(ChatRole.User, text));
            var response = await this.client.GetResponseAsync(
                this.history,
                new ChatOptions { Tools = [.. this.tools] }
            );
            this.history.AddMessages(response);

            var reply = string.IsNullOrWhiteSpace(response.Text) ? "(done)" : response.Text;
            Messages.Add(new AiMessage(false, reply));
        }
        catch (Exception ex)
        {
            Messages.Add(new AiMessage(false, "⚠ " + ex.Message));
        }
        finally
        {
            this.IsBusy = false;
        }
    }
}

/// <summary>A single chat bubble. Colours/alignment are precomputed so the view binds directly.</summary>
public class AiMessage
{
    public AiMessage(bool isUser, string text)
    {
        this.IsUser = isUser;
        this.Text = text;

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        this.Align = isUser ? LayoutOptions.End : LayoutOptions.Start;
        this.Background = isUser
            ? Color.FromArgb("#6C5CE7")
            : (isDark ? Color.FromArgb("#2A2A3E") : Color.FromArgb("#FFFFFF"));
        this.TextColor = isUser
            ? Colors.White
            : (isDark ? Colors.White : Color.FromArgb("#212121"));
    }

    public bool IsUser { get; }
    public string Text { get; }
    public LayoutOptions Align { get; }
    public Color Background { get; }
    public Color TextColor { get; }
}
