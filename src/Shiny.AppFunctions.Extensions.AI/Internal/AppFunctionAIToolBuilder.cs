namespace Shiny.AppFunctions.Extensions.AI.Internal;

sealed class AppFunctionAIToolBuilder : IAppFunctionAIToolBuilder
{
    public bool All { get; private set; }
    public List<string> FunctionIds { get; } = [];
    public HashSet<string> Excluded { get; } = [];

    public IAppFunctionAIToolBuilder AddAllFunctions()
    {
        this.All = true;
        return this;
    }

    public IAppFunctionAIToolBuilder AddFunction(string functionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        if (!this.FunctionIds.Contains(functionId))
            this.FunctionIds.Add(functionId);
        return this;
    }

    public IAppFunctionAIToolBuilder AddFunctions(IEnumerable<string> functionIds)
    {
        ArgumentNullException.ThrowIfNull(functionIds);
        foreach (var id in functionIds)
            this.AddFunction(id);
        return this;
    }

    public IAppFunctionAIToolBuilder ExcludeFunction(string functionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        this.Excluded.Add(functionId);
        return this;
    }
}
