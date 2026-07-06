using Microsoft.Extensions.AI;

namespace Shiny.Notifications.Extensions.AI;

/// <summary>
/// Bundle of <see cref="AITool"/> instances generated for the reminder operations you opt-in to via
/// <c>AddNotificationAITools</c>. Resolve this from DI and pass <see cref="Tools"/> to your
/// <c>IChatClient</c> call (e.g. <c>ChatOptions.Tools</c>).
/// </summary>
public sealed class NotificationAITools
{
    /// <summary>The generated tools. Operations not opted-in are invisible to the LLM.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    internal NotificationAITools(IReadOnlyList<AITool> tools) => this.Tools = tools;
}
