using Microsoft.Extensions.AI;

namespace Shiny.Locations.Extensions.AI;

/// <summary>
/// Bundle of read-only GPS <see cref="AITool"/> instances registered via
/// <c>AddLocationAITool</c>. Resolve this from DI and pass <see cref="Tools"/> to your
/// <c>IChatClient</c> call (e.g. <c>ChatOptions.Tools</c>).
/// </summary>
public sealed class LocationAITools
{
    /// <summary>The generated read-only GPS tools exposed to the LLM.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    internal LocationAITools(IReadOnlyList<AITool> tools) => this.Tools = tools;
}
