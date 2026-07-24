using Microsoft.Extensions.AI;

namespace Shiny.Calendar.Extensions.AI;

/// <summary>
/// Bundle of <see cref="AITool"/> instances generated for the calendar operations you opt-in to via
/// <c>AddCalendarAITools</c>. Resolve this from DI and pass <see cref="Tools"/> to your
/// <c>IChatClient</c> call (e.g. <c>ChatOptions.Tools</c>).
/// </summary>
public sealed class CalendarAITools
{
    /// <summary>The generated tools. Operations not opted-in are invisible to the LLM.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    internal CalendarAITools(IReadOnlyList<AITool> tools) => this.Tools = tools;
}
