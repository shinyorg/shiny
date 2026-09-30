using Microsoft.Extensions.AI;

namespace Shiny.AppFunctions.Extensions.AI;

/// <summary>
/// Bundle of <see cref="AITool"/> instances wrapping the app functions you opt-in to via
/// <c>AddAppFunctionAITools</c>. Resolve this from DI and pass <see cref="Tools"/> to your
/// <c>IChatClient</c> call (e.g. <c>ChatOptions.Tools</c>).
/// </summary>
public sealed class AppFunctionAITools
{
    /// <summary>The tools, one per app function. Functions not opted-in are invisible to the LLM.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    internal AppFunctionAITools(IReadOnlyList<AITool> tools) => this.Tools = tools;
}
