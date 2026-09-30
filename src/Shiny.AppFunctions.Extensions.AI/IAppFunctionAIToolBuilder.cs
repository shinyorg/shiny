namespace Shiny.AppFunctions.Extensions.AI;

/// <summary>
/// Opt-in builder for the app functions an AI agent is allowed to call. Nothing is exposed to the LLM
/// unless you add it here. The functions are the ones Siri and Gemini call - declared with
/// <see cref="AppFunctionAttribute"/> and registered by <c>AddAppFunctions()</c>.
/// </summary>
public interface IAppFunctionAIToolBuilder
{
    /// <summary>Allows the AI agent to call every app function, including the generated <c>search_{entity}</c> functions.</summary>
    IAppFunctionAIToolBuilder AddAllFunctions();

    /// <summary>
    /// Allows the AI agent to call one app function. When it takes an <see cref="AppEntityAttribute"/> parameter,
    /// that entity's <c>search_{entity}</c> function is added too, so the agent can look up an id first.
    /// </summary>
    /// <param name="functionId">The <see cref="AppFunctionAttribute.Id"/>, e.g. <c>create_order</c>.</param>
    IAppFunctionAIToolBuilder AddFunction(string functionId);

    /// <summary>Equivalent to calling <see cref="AddFunction"/> for each id.</summary>
    /// <param name="functionIds">The <see cref="AppFunctionAttribute.Id"/> values.</param>
    IAppFunctionAIToolBuilder AddFunctions(IEnumerable<string> functionIds);

    /// <summary>
    /// Hides an app function from the AI agent even when <see cref="AddAllFunctions"/> or an entity parameter
    /// would have added it - for example, everything except <c>cancel_order</c>.
    /// </summary>
    /// <param name="functionId">The <see cref="AppFunctionAttribute.Id"/> to hide.</param>
    IAppFunctionAIToolBuilder ExcludeFunction(string functionId);
}
